using Microsoft.EntityFrameworkCore;
using SistemasPrecios.Api.Data;
using SistemasPrecios.Api.Dtos;
namespace SistemasPrecios.Api.Services;
internal static class DocumentQueryFactory
{
    public static async Task<DocumentDetailResponse?> BuildDetailAsync(
        ApplicationDbContext db,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var document = await db.Documents
            .Include(document => document.Supplier)
            .Include(document => document.ExtractedLines)
            .ThenInclude(line => line.Matches)
            .ThenInclude(match => match.CanonicalProduct)
            .AsNoTracking()
            .FirstOrDefaultAsync(document => document.Id == documentId, cancellationToken);
        if (document is null) return null;
        return new DocumentDetailResponse(
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
                            .ToList(), line.SuggestedCurrency, line.ApprovedCurrency, line.Excluded))
                    .ToList(), document.SourceKind, document.ObservedAt);
    }
}
