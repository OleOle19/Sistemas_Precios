using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SistemasPrecios.Api.Data;
using SistemasPrecios.Api.Domain;
using SistemasPrecios.Api.Dtos;
using SistemasPrecios.Api.Services;

namespace SistemasPrecios.Api.Endpoints;

public static class ApiRoutes
{
    public static void MapApiRoutes(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", LoginAsync);
        app.MapPost("/auth/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        }).RequireAuthorization();

        app.MapGet("/suppliers", GetSuppliersAsync).RequireAuthorization();
        app.MapPost("/suppliers", CreateSupplierAsync).RequireAuthorization();

        app.MapGet("/documents", GetDocumentsAsync).RequireAuthorization();
        app.MapGet("/documents/{id:guid}", GetDocumentAsync).RequireAuthorization();
        app.MapPost("/documents", UploadDocumentAsync).RequireAuthorization();
        app.MapPost("/documents/{id:guid}/reprocess", ReprocessDocumentAsync).RequireAuthorization();
        app.MapPost("/documents/{id:guid}/review", ReviewDocumentAsync).RequireAuthorization();

        app.MapGet("/comparisons/current", GetCurrentComparisonsAsync).RequireAuthorization();
        app.MapGet("/comparisons/history", GetHistoryAsync).RequireAuthorization();
        app.MapPost("/matches/{id:guid}/approve", ApproveMatchAsync).RequireAuthorization();
        app.MapGet("/dashboard/summary", GetDashboardSummaryAsync).RequireAuthorization();
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        ApplicationDbContext db,
        IPasswordService passwordService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(item => item.Email == request.Email, cancellationToken);
        if (user is null || !passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        return Results.Ok(new LoggedInUserResponse(user.Id, user.FullName, user.Email, user.Role.ToString()));
    }

    private static async Task<IResult> GetSuppliersAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var suppliers = await db.Suppliers
            .OrderBy(supplier => supplier.Name)
            .Select(supplier => new SupplierResponse(
                supplier.Id,
                supplier.Name,
                supplier.ContactEmail,
                supplier.Documents.Count))
            .ToListAsync(cancellationToken);

        return Results.Ok(suppliers);
    }

    private static async Task<IResult> CreateSupplierAsync(
        SupplierCreateRequest request,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var supplier = new Supplier
        {
            Name = request.Name.Trim(),
            ContactEmail = request.ContactEmail?.Trim()
        };

        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/suppliers/{supplier.Id}", new SupplierResponse(
            supplier.Id,
            supplier.Name,
            supplier.ContactEmail,
            0));
    }

    private static async Task<IResult> GetDocumentsAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var documents = await db.Documents
            .Include(document => document.Supplier)
            .Include(document => document.ExtractedLines)
            .OrderByDescending(document => document.UploadedAt)
            .Select(document => new DocumentSummaryResponse(
                document.Id,
                document.SupplierId,
                document.Supplier.Name,
                document.FileName,
                document.ContentType,
                document.Status.ToString(),
                document.UploadedAt,
                document.ExtractedLines.Count,
                document.FailureReason))
            .ToListAsync(cancellationToken);

        return Results.Ok(documents);
    }

    private static async Task<IResult> GetDocumentAsync(
        Guid id,
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var document = await DocumentQueryFactory.BuildDetailAsync(db, id, cancellationToken);
        return document is null ? Results.NotFound() : Results.Ok(document);
    }

    private static async Task<IResult> UploadDocumentAsync(
        HttpRequest request,
        ApplicationDbContext db,
        IDocumentJobScheduler documentJobScheduler,
        IOptions<StorageOptions> storageOptions,
        CancellationToken cancellationToken)
    {
        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files["file"];
        var supplierIdRaw = form["supplierId"].ToString();
        var ocrText = form["ocrText"].ToString();

        if (file is null || file.Length == 0 || !Guid.TryParse(supplierIdRaw, out var supplierId))
        {
            return Results.BadRequest(new { message = "Debes enviar supplierId y un archivo valido." });
        }

        var supplierExists = await db.Suppliers.AnyAsync(supplier => supplier.Id == supplierId, cancellationToken);
        if (!supplierExists)
        {
            return Results.BadRequest(new { message = "Proveedor no encontrado." });
        }

        var allowedContentTypes = new[]
        {
            "image/jpeg",
            "image/png",
            "image/webp",
            "application/pdf"
        };

        if (!allowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { message = "Solo se permiten fotos y PDFs." });
        }

        var rootPath = Path.GetFullPath(storageOptions.Value.RootPath, Directory.GetCurrentDirectory());
        Directory.CreateDirectory(rootPath);
        var datedFolder = Path.Combine(rootPath, DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
        Directory.CreateDirectory(datedFolder);

        var storedFileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(datedFolder, storedFileName);

        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(ocrText))
        {
            var sidecarPath = Path.ChangeExtension(filePath, ".txt");
            await File.WriteAllTextAsync(sidecarPath, ocrText.Trim(), cancellationToken);
        }

        var document = new Document
        {
            SupplierId = supplierId,
            FileName = file.FileName,
            StoredFileName = storedFileName,
            ContentType = file.ContentType,
            FilePath = filePath,
            Status = DocumentStatus.Uploaded
        };

        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        await documentJobScheduler.QueueProcessingAsync(document.Id, cancellationToken);

        return Results.Created($"/documents/{document.Id}", new DocumentSummaryResponse(
            document.Id,
            document.SupplierId,
            (await db.Suppliers.Where(s => s.Id == supplierId).Select(s => s.Name).FirstAsync(cancellationToken)),
            document.FileName,
            document.ContentType,
            document.Status.ToString(),
            document.UploadedAt,
            0,
            null));
    }

    private static async Task<IResult> ReprocessDocumentAsync(
        Guid id,
        ApplicationDbContext db,
        IDocumentJobScheduler documentJobScheduler,
        CancellationToken cancellationToken)
    {
        var document = await db.Documents.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (document is null)
        {
            return Results.NotFound();
        }

        document.Status = DocumentStatus.Uploaded;
        document.FailureReason = null;
        await db.SaveChangesAsync(cancellationToken);

        await documentJobScheduler.QueueProcessingAsync(document.Id, cancellationToken);
        return Results.Accepted($"/documents/{document.Id}", new { message = "Documento encolado para reproceso." });
    }

    private static async Task<IResult> ReviewDocumentAsync(
        Guid id,
        DocumentReviewRequest request,
        IDocumentProcessingService documentProcessingService,
        CancellationToken cancellationToken)
    {
        var detail = await documentProcessingService.ReviewDocumentAsync(id, request, cancellationToken);
        return Results.Ok(detail);
    }

    private static async Task<IResult> GetCurrentComparisonsAsync(
        IComparisonQueryService comparisonQueryService,
        CancellationToken cancellationToken)
    {
        var result = await comparisonQueryService.GetCurrentComparisonsAsync(cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetHistoryAsync(
        Guid? productId,
        Guid? supplierId,
        IComparisonQueryService comparisonQueryService,
        CancellationToken cancellationToken)
    {
        var result = await comparisonQueryService.GetHistoryAsync(productId, supplierId, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> ApproveMatchAsync(
        Guid id,
        IDocumentProcessingService documentProcessingService,
        CancellationToken cancellationToken)
    {
        await documentProcessingService.ApproveMatchAsync(id, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetDashboardSummaryAsync(
        IComparisonQueryService comparisonQueryService,
        CancellationToken cancellationToken)
    {
        var summary = await comparisonQueryService.GetDashboardSummaryAsync(cancellationToken);
        return Results.Ok(summary);
    }
}
