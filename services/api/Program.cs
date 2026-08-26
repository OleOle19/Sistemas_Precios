using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using SistemasPrecios.Api.Data;
using SistemasPrecios.Api.Endpoints;
using SistemasPrecios.Api.Services;

var builder = WebApplication.CreateBuilder(args);
var databaseProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var usePostgres = string.Equals(databaseProvider, "Postgres", StringComparison.OrdinalIgnoreCase);
EnsureLocalSqliteDirectory(builder.Configuration, usePostgres);

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<MatchingOptions>(builder.Configuration.GetSection(MatchingOptions.SectionName));

builder.Services.AddProblemDetails();

builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://127.0.0.1:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "sistemas-precios-session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/auth/login";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (usePostgres)
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

if (usePostgres)
{
    builder.Services.AddHangfire(configuration =>
    {
        configuration
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c =>
                c.UseNpgsqlConnection(
                    builder.Configuration.GetConnectionString("DefaultConnection")));
    });
    builder.Services.AddHangfireServer();
    builder.Services.AddScoped<IDocumentJobScheduler, HangfireDocumentJobScheduler>();
}
else
{
    builder.Services.AddScoped<IDocumentJobScheduler, InlineDocumentJobScheduler>();
}

var visionUrl = builder.Configuration["Vision:GrpcUrl"] ?? "http://localhost:50051";
builder.Services.AddGrpcClient<Vision.VisionProcessor.VisionProcessorClient>(options =>
{
    options.Address = new Uri(visionUrl);
});

builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IVisionExtractionClient, GrpcVisionExtractionClient>();
builder.Services.AddScoped<IExtractionStructurer, MockExtractionStructurer>();
builder.Services.AddScoped<IProductMatchingService, ProductMatchingService>();
builder.Services.AddScoped<IComparisonQueryService, ComparisonQueryService>();
builder.Services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
if (usePostgres)
{
    app.UseHangfireDashboard("/hangfire");
}

app.MapGet("/healthz", () => Results.Ok(new
{
    status = "ok",
    service = "SistemasPrecios.Api"
}));

app.MapApiRoutes();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
    if (db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
    {
        await EnsureApplicationSchemaAsync(db);
    }
    await SeedDataService.InitializeAsync(scope.ServiceProvider);
}

app.Run();

static void EnsureLocalSqliteDirectory(ConfigurationManager configuration, bool usePostgres)
{
    if (usePostgres)
    {
        return;
    }

    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return;
    }

    var sqliteBuilder = new SqliteConnectionStringBuilder(connectionString);
    if (string.IsNullOrWhiteSpace(sqliteBuilder.DataSource))
    {
        return;
    }

    var fullDatabasePath = Path.GetFullPath(sqliteBuilder.DataSource, Directory.GetCurrentDirectory());
    var directory = Path.GetDirectoryName(fullDatabasePath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }
}

static async Task EnsureApplicationSchemaAsync(ApplicationDbContext db)
{
    var connection = db.Database.GetDbConnection();
    await connection.OpenAsync();

    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass('public.\"Users\"') IS NOT NULL;";
        var result = await command.ExecuteScalarAsync();
        var usersTableExists = result is bool flag && flag;

        if (!usersTableExists)
        {
            var creator = db.GetService<IRelationalDatabaseCreator>();
            await creator.CreateTablesAsync();
        }
    }
    finally
    {
        await connection.CloseAsync();
    }
}

public partial class Program;
