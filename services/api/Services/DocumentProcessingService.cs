using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SistemasPrecios.Api.Data;
using SistemasPrecios.Api.Domain;
using SistemasPrecios.Api.Dtos;

namespace SistemasPrecios.Api.Services;

public interface IDocumentProcessingService
{
    Task ProcessDocumentAsync(Guid documentId);
    Task<DocumentDetailResponse> ReviewDocumentAsync(Guid documentId, DocumentReviewRequest request, CancellationToken cancellationToken);
    Task ApproveMatchAsync(Guid matchId, CancellationToken cancellationToken);
}

public sealed class DocumentProcessingService(
    ApplicationDbContext db,
    IVisionExtractionClient visionExtractionClient,
    IExtractionStructurer extractionStructurer,
    IProductMatchingService productMatchingService,
    IComparisonQueryService comparisonQueryService,
    IOptions<MatchingOptions> matchingOptions) : IDocumentProcessingService
{
    private readonly ApplicationDbContext _db = db;
    private readonly IVisionExtractionClient _visionExtractionClient = visionExtractionClient;
    private readonly IExtractionStructurer _extractionStructurer = extractionStructurer;
    private readonly IProductMatchingService _productMatchingService = productMatchingService;
    private readonly IComparisonQueryService _comparisonQueryService = comparisonQueryService;
    private readonly MatchingOptions _matchingOptions = matchingOptions.Value;

    public async Task ProcessDocumentAsync(Guid documentId)
    {
        var document = await _db.Documents
            .Include(item => item.ExtractedLines)
            .ThenInclude(line => line.Matches)
            .FirstOrDefaultAsync(item => item.Id == documentId);

        if (document is null)
        {
            return;
        }

        var job = new ProcessingJob
        {
            DocumentId = document.Id,
            JobType = "extract",
            Status = JobStatus.Running
        };

        _db.ProcessingJobs.Add(job);

        try
        {
            document.Status = DocumentStatus.Processing;
            document.FailureReason = null;

            if (document.ExtractedLines.Count > 0)
            {
                _db.ProductMatches.RemoveRange(document.ExtractedLines.SelectMany(line => line.Matches));
                _db.ExtractedLines.RemoveRange(document.ExtractedLines);
            }

            var oldSnapshots = await _db.PriceSnapshots
                .Where(snapshot => snapshot.DocumentId == documentId)
                .ToListAsync();

            if (oldSnapshots.Count > 0)
            {
                _db.PriceSnapshots.RemoveRange(oldSnapshots);
            }

            await _db.SaveChangesAsync();

            var blocks = await _visionExtractionClient.ProcessDocumentAsync(
                document.FilePath,
                document.ContentType,
                CancellationToken.None);

            var structuredLines = await _extractionStructurer.StructureAsync(blocks, CancellationToken.None);

            if (structuredLines.Count == 0)
            {
                document.Status = DocumentStatus.Failed;
                document.FailureReason = "No se detectaron lineas utiles en el documento.";
                job.Status = JobStatus.Failed;
                job.ErrorMessage = document.FailureReason;
                job.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return;
            }

            var hasPendingReview = false;

            foreach (var line in structuredLines)
            {
                var candidates = await _productMatchingService.FindCandidatesAsync(line, CancellationToken.None);
                var topCandidate = candidates.FirstOrDefault();
                var needsReview =
                    line.ConfidenceScore < _matchingOptions.ReviewThreshold ||
                    line.SuggestedPrice is null ||
                    topCandidate is null ||
                    topCandidate.ConfidenceScore < _matchingOptions.AutoApproveThreshold;

                var extractedLine = new ExtractedLine
                {
                    DocumentId = document.Id,
                    LineNumber = line.LineNumber,
                    RawText = line.RawText,
                    SuggestedName = line.SuggestedName,
                    SuggestedUnit = line.SuggestedUnit,
                    SuggestedQuantity = line.SuggestedQuantity,
                    SuggestedPrice = line.SuggestedPrice,
                    ConfidenceScore = line.ConfidenceScore,
                    NeedsReview = needsReview,
                    ApprovedCanonicalProductId = !needsReview ? topCandidate?.CanonicalProductId : null,
                    ApprovedCanonicalProductName = !needsReview ? topCandidate?.CanonicalProductName : null,
                    ApprovedQuantity = !needsReview ? line.SuggestedQuantity : null,
                    ApprovedUnit = !needsReview ? line.SuggestedUnit : null,
                    ApprovedPrice = !needsReview ? line.SuggestedPrice : null,
                    Matches = candidates.Select(candidate => new ProductMatch
                    {
                        CanonicalProductId = candidate.CanonicalProductId,
                        ConfidenceScore = candidate.ConfidenceScore,
                        Status = !needsReview && candidate.CanonicalProductId == topCandidate?.CanonicalProductId
                            ? MatchStatus.Approved
                            : MatchStatus.Suggested
                    }).ToList()
                };

                hasPendingReview |= needsReview;
                _db.ExtractedLines.Add(extractedLine);
            }

            document.Status = hasPendingReview
                ? DocumentStatus.NeedsReview
                : DocumentStatus.Processed;

            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            document.Status = DocumentStatus.Failed;
            document.FailureReason = exception.Message;
            job.Status = JobStatus.Failed;
            job.ErrorMessage = exception.Message;
            job.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<DocumentDetailResponse> ReviewDocumentAsync(
        Guid documentId,
        DocumentReviewRequest request,
        CancellationToken cancellationToken)
    {
        var document = await _db.Documents
            .Include(item => item.Supplier)
            .Include(item => item.ExtractedLines)
            .ThenInclude(line => line.Matches)
            .FirstOrDefaultAsync(item => item.Id == documentId, cancellationToken);

        if (document is null)
        {
            throw new InvalidOperationException("Documento no encontrado.");
        }

        var snapshots = await _db.PriceSnapshots
            .Where(snapshot => snapshot.DocumentId == document.Id)
            .ToListAsync(cancellationToken);
        _db.PriceSnapshots.RemoveRange(snapshots);

        var affectedProductIds = new List<Guid>();

        foreach (var reviewLine in request.Lines)
        {
            var extractedLine = document.ExtractedLines.FirstOrDefault(line => line.Id == reviewLine.ExtractedLineId);
            if (extractedLine is null)
            {
                continue;
            }

            var canonicalName = reviewLine.CanonicalName.Trim();
            var canonicalProduct = await _db.CanonicalProducts
                .Include(product => product.Aliases)
                .FirstOrDefaultAsync(
                    product => product.Name.ToLower() == canonicalName.ToLower(),
                    cancellationToken);

            if (canonicalProduct is null)
            {
                canonicalProduct = new Domain.CanonicalProduct
                {
                    Name = canonicalName,
                    BaseUnit = reviewLine.Unit
                };
                _db.CanonicalProducts.Add(canonicalProduct);
            }

            var suggestedAlias = extractedLine.SuggestedName ?? extractedLine.RawText;
            if (!canonicalProduct.Aliases.Any(alias =>
                    ProductMatchingService.Normalize(alias.Alias) ==
                    ProductMatchingService.Normalize(suggestedAlias)))
            {
                canonicalProduct.Aliases.Add(new Domain.ProductAlias
                {
                    Alias = suggestedAlias
                });
            }

            extractedLine.ApprovedCanonicalProductId = canonicalProduct.Id;
            extractedLine.ApprovedCanonicalProductName = canonicalProduct.Name;
            extractedLine.ApprovedPrice = reviewLine.Price;
            extractedLine.ApprovedQuantity = reviewLine.Quantity;
            extractedLine.ApprovedUnit = reviewLine.Unit;
            extractedLine.NeedsReview = false;

            foreach (var match in extractedLine.Matches)
            {
                match.Status = match.CanonicalProductId == canonicalProduct.Id
                    ? MatchStatus.Approved
                    : MatchStatus.Rejected;
            }

            _db.PriceSnapshots.Add(new PriceSnapshot
            {
                SupplierId = document.SupplierId,
                CanonicalProductId = canonicalProduct.Id,
                DocumentId = document.Id,
                Price = reviewLine.Price,
                Quantity = reviewLine.Quantity,
                Unit = reviewLine.Unit,
                EffectiveAt = document.UploadedAt
            });

            affectedProductIds.Add(canonicalProduct.Id);
        }

        document.Status = DocumentStatus.Approved;
        document.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _comparisonQueryService.RefreshAsync(affectedProductIds, cancellationToken);

        return await DocumentQueryFactory.BuildDetailAsync(_db, documentId, cancellationToken)
               ?? throw new InvalidOperationException("No se pudo reconstruir el documento.");
    }

    public async Task ApproveMatchAsync(Guid matchId, CancellationToken cancellationToken)
    {
        var match = await _db.ProductMatches
            .Include(item => item.CanonicalProduct)
            .Include(item => item.ExtractedLine)
            .ThenInclude(line => line.Matches)
            .FirstOrDefaultAsync(item => item.Id == matchId, cancellationToken);

        if (match is null)
        {
            throw new InvalidOperationException("Match no encontrado.");
        }

        foreach (var siblingMatch in match.ExtractedLine.Matches)
        {
            siblingMatch.Status = siblingMatch.Id == match.Id
                ? MatchStatus.Approved
                : MatchStatus.Rejected;
        }

        match.ExtractedLine.ApprovedCanonicalProductId = match.CanonicalProductId;
        match.ExtractedLine.ApprovedCanonicalProductName = match.CanonicalProduct.Name;
        match.ExtractedLine.ApprovedPrice ??= match.ExtractedLine.SuggestedPrice;
        match.ExtractedLine.ApprovedQuantity ??= match.ExtractedLine.SuggestedQuantity;
        match.ExtractedLine.ApprovedUnit ??= match.ExtractedLine.SuggestedUnit;
        match.ExtractedLine.NeedsReview = match.ExtractedLine.ApprovedPrice is null;

        await _db.SaveChangesAsync(cancellationToken);
    }
}

internal static class DocumentQueryFactory
{
    public static async Task<DocumentDetailResponse?> BuildDetailAsync(
        ApplicationDbContext db,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        return await db.Documents
            .Include(document => document.Supplier)
            .Include(document => document.ExtractedLines)
            .ThenInclude(line => line.Matches)
            .ThenInclude(match => match.CanonicalProduct)
            .Where(document => document.Id == documentId)
            .Select(document => new DocumentDetailResponse(
                document.Id,
                document.SupplierId,
                document.Supplier.Name,
                document.FileName,
                document.ContentType,
                document.Status.ToString(),
                document.UploadedAt,
                document.ExtractedLines.Count,
                document.FailureReason,
                document.ExtractedLines
                    .OrderBy(line => line.LineNumber)
                    .Select(line => new ExtractedLineResponse(
                        line.Id,
                        line.LineNumber,
                        line.RawText,
                        line.SuggestedName,
                        line.SuggestedUnit,
                        line.SuggestedQuantity,
                        line.SuggestedPrice,
                        line.ConfidenceScore,
                        line.NeedsReview,
                        line.ApprovedCanonicalProductId,
                        line.ApprovedCanonicalProductName,
                        line.ApprovedQuantity,
                        line.ApprovedUnit,
                        line.ApprovedPrice,
                        line.Matches
                            .OrderByDescending(match => match.ConfidenceScore)
                            .Select(match => new ProductMatchResponse(
                                match.Id,
                                match.CanonicalProductId,
                                match.CanonicalProduct.Name,
                                match.ConfidenceScore,
                                match.Status.ToString()))
                            .ToList()))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
