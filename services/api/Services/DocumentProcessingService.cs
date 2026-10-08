using Microsoft.EntityFrameworkCore;
using SistemasPrecios.Api.Data;
using SistemasPrecios.Api.Domain;
using SistemasPrecios.Api.Dtos;
namespace SistemasPrecios.Api.Services;
public interface IDocumentProcessingService
{
    Task ProcessDocumentAsync(Guid documentId);
    Task<DocumentDetailResponse> ReviewDocumentAsync(Guid documentId, DocumentReviewRequest request, CancellationToken cancellationToken);
}
public sealed class DocumentProcessingService(ApplicationDbContext db, IPriceExtractor extractor, IProductMatchingService matching, ILogger<DocumentProcessingService> logger) : IDocumentProcessingService
{
    public async Task ProcessDocumentAsync(Guid documentId)
    {
        var document = await db.Documents.Include(d => d.ExtractedLines).ThenInclude(l => l.Matches).SingleOrDefaultAsync(d => d.Id == documentId);
        if (document is null || document.Status != DocumentStatus.Uploaded) return;
        document.Status = DocumentStatus.Processing; document.FailureReason = null;
        var job = new ProcessingJob { DocumentId = documentId, Status = JobStatus.Running };
        db.ProcessingJobs.Add(job); await db.SaveChangesAsync();
        try
        {
            var lines = await extractor.ExtractAsync(document.FilePath, document.ContentType, document.SourceKind, CancellationToken.None);
            if (lines.Count == 0) throw new ExtractionException("No se encontraron precios. Usa una foto más clara.");
            db.ProductMatches.RemoveRange(document.ExtractedLines.SelectMany(l => l.Matches)); db.ExtractedLines.RemoveRange(document.ExtractedLines);
            foreach (var line in lines)
            {
                var candidates = await matching.FindCandidatesAsync(line, CancellationToken.None);
                db.ExtractedLines.Add(new ExtractedLine
                {
                    DocumentId = documentId,
                    LineNumber = line.LineNumber,
                    RawText = line.RawText,
                    SuggestedName = line.SuggestedName,
                    SuggestedUnit = line.SuggestedUnit,
                    SuggestedQuantity = line.SuggestedQuantity,
                    SuggestedPrice = line.SuggestedPrice,
                    SuggestedCurrency = line.SuggestedCurrency,
                    NeedsReview = true,
                    Matches = candidates.Select(c => new ProductMatch { CanonicalProductId = c.CanonicalProductId, ConfidenceScore = c.ConfidenceScore }).ToList()
                });
            }
            document.Status = DocumentStatus.NeedsReview; job.Status = JobStatus.Completed;
        }
        catch (Exception ex)
        {
            logger.LogWarning("La lectura {DocumentId} falló: {ErrorType}", documentId, ex.GetType().Name);
            db.ChangeTracker.Clear(); document = await db.Documents.SingleAsync(d => d.Id == documentId); job = await db.ProcessingJobs.SingleAsync(j => j.Id == job.Id);
            document.Status = DocumentStatus.Failed; document.FailureReason = ex is ExtractionException ? ex.Message : "No se pudo completar la lectura. Revisa la conexión y el archivo antes de reintentar.";
            job.Status = JobStatus.Failed; job.ErrorMessage = document.FailureReason;
        }
        job.CompletedAt = DateTime.UtcNow; await db.SaveChangesAsync();
    }
    public async Task<DocumentDetailResponse> ReviewDocumentAsync(Guid id, DocumentReviewRequest request, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var document = await db.Documents.Include(d => d.ExtractedLines).ThenInclude(l => l.Matches).SingleOrDefaultAsync(d => d.Id == id, ct) ?? throw new ArgumentException("Documento no encontrado.");
        if (document.Status != DocumentStatus.NeedsReview) throw new ArgumentException("Solo puedes aprobar documentos pendientes de revisión.");
        var ids = request.Lines?.Select(l => l.ExtractedLineId).ToList();
        if (ids is null || ids.Count == 0 || ids.Distinct().Count() != ids.Count || !ids.Order().SequenceEqual(document.ExtractedLines.Select(l => l.Id).Order()))
            throw new ArgumentException("Revisa todas las filas una sola vez. Puedes descartar las que no sean precios.");
        foreach (var line in request.Lines!)
        {
            if (line.Excluded) continue;
            if (string.IsNullOrWhiteSpace(line.CanonicalName) || line.CanonicalName.Trim().Length > 250 || line.Price <= 0 || line.Price > 1_000_000 || line.Quantity <= 0 || line.Quantity > 1_000_000 || !new[] { "PEN", "USD", "EUR" }.Contains(line.Currency))
                throw new ArgumentException("Completa producto, moneda, precio positivo y contenido positivo en todas las filas incluidas.");
            if (UnitNormalizer.Normalize(line.Unit ?? "", line.Quantity).Quantity < 0.000001m || decimal.Round(line.Price / UnitNormalizer.Normalize(line.Unit ?? "", line.Quantity).Quantity, 6) <= 0) throw new ArgumentException("El contenido es demasiado pequeño.");
        }
        if (request.Lines!.All(l => l.Excluded)) throw new ArgumentException("Incluye al menos un precio para aprobar.");
        var products = await db.CanonicalProducts.Include(p => p.Aliases).ToListAsync(ct); var now = DateTime.UtcNow; var included = new HashSet<(Guid, string)>();
        foreach (var input in request.Lines!)
        {
            var line = document.ExtractedLines.Single(l => l.Id == input.ExtractedLineId); line.Excluded = input.Excluded; line.NeedsReview = false;
            if (input.Excluded) continue;
            var (unit, quantity) = UnitNormalizer.Normalize(input.Unit, input.Quantity); var name = input.CanonicalName.Trim();
            var product = products.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (product is null) { product = new CanonicalProduct { Name = name, BaseUnit = unit }; products.Add(product); db.CanonicalProducts.Add(product); }
            if (product.BaseUnit != unit) throw new ArgumentException($"El producto '{name}' usa {product.BaseUnit}. Usa otra identidad si no es equivalente.");
            if (!included.Add((product.Id, input.Currency))) throw new ArgumentException("Hay dos precios del mismo producto y moneda en este archivo. Descarta la fila duplicada o distingue correctamente los productos.");
            var alias = line.SuggestedName;
            if (!string.IsNullOrWhiteSpace(alias) && alias.Length <= 300 && !product.Aliases.Any(a => a.Alias == alias)) product.Aliases.Add(new ProductAlias { Alias = alias });
            line.ApprovedCanonicalProductId = product.Id; line.ApprovedCanonicalProductName = product.Name; line.ApprovedPrice = input.Price;
            line.ApprovedQuantity = input.Quantity; line.ApprovedUnit = input.Unit.Trim().ToUpperInvariant(); line.ApprovedCurrency = input.Currency;
            foreach (var match in line.Matches) match.Status = match.CanonicalProductId == product.Id ? MatchStatus.Approved : MatchStatus.Rejected;
            db.PriceSnapshots.Add(new PriceSnapshot
            {
                DocumentId = id,
                SupplierId = document.SupplierId,
                CanonicalProductId = product.Id,
                Quantity = quantity,
                Unit = unit,
                Price = decimal.Round(input.Price / quantity, 6),
                Currency = input.Currency,
                EffectiveAt = document.ObservedAt,
                RecordedAt = now
            });
        }
        document.Status = DocumentStatus.Approved; document.ReviewedAt = now; document.Revision = Guid.NewGuid(); await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return (await DocumentQueryFactory.BuildDetailAsync(db, id, ct))!;
    }
}
