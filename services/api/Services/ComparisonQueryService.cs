using Microsoft.EntityFrameworkCore;
using SistemasPrecios.Api.Data;
using SistemasPrecios.Api.Domain;
using SistemasPrecios.Api.Dtos;
namespace SistemasPrecios.Api.Services;
public interface IComparisonQueryService
{
    Task<IReadOnlyList<CurrentComparisonItemDto>> GetCurrentComparisonsAsync(CancellationToken ct);
    Task<IReadOnlyList<ComparisonHistoryItemDto>> GetHistoryAsync(Guid? productId, Guid? supplierId, CancellationToken ct);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct);
}
public sealed class ComparisonQueryService(ApplicationDbContext db) : IComparisonQueryService
{
    public async Task<IReadOnlyList<CurrentComparisonItemDto>> GetCurrentComparisonsAsync(CancellationToken ct)
    {
        var snapshots = await db.PriceSnapshots.AsNoTracking().Include(s => s.CanonicalProduct).Include(s => s.Supplier).ToListAsync(ct);
        return snapshots.GroupBy(s => new { s.CanonicalProductId, s.Currency, s.Unit }).Select(group =>
        {
            var latest = group.GroupBy(s => s.SupplierId).Select(g => g.OrderByDescending(s => s.EffectiveAt).ThenByDescending(s => s.RecordedAt).ThenBy(s => s.Id).First()).ToList();
            var best = latest.OrderBy(s => s.Price).ThenBy(s => s.Supplier.Name).First(); var avg = latest.Average(s => s.Price); var high = latest.Max(s => s.Price);
            return new CurrentComparisonItemDto(best.CanonicalProductId, best.CanonicalProduct.Name, best.Unit, best.Supplier.Name, best.Price,
       decimal.Round(avg, 6), high, decimal.Round((high - best.Price) / avg * 100, 2), latest.Max(s => s.RecordedAt), best.Currency, latest.Count);
        }).OrderByDescending(c => c.SpreadPercentage).ToList();
    }
    public async Task<IReadOnlyList<ComparisonHistoryItemDto>> GetHistoryAsync(Guid? productId, Guid? supplierId, CancellationToken ct)
    {
        var query = db.PriceSnapshots.AsNoTracking().Include(s => s.CanonicalProduct).Include(s => s.Supplier).AsQueryable();
        if (productId.HasValue) query = query.Where(s => s.CanonicalProductId == productId); if (supplierId.HasValue) query = query.Where(s => s.SupplierId == supplierId);
        var snapshots = await query.OrderBy(s => s.EffectiveAt).ThenBy(s => s.RecordedAt).ThenBy(s => s.Id).ToListAsync(ct);
        var previous = new Dictionary<(Guid, Guid, string, string), decimal>(); var history = new List<ComparisonHistoryItemDto>();
        foreach (var s in snapshots)
        {
            var key = (s.CanonicalProductId, s.SupplierId, s.Currency, s.Unit);
            var hasPrevious = previous.TryGetValue(key, out var p);
            var variation = hasPrevious ? PriceMath.CalculateVariation(s.Price, p) : null;
            history.Add(new(s.CanonicalProduct.Name, s.Supplier.Name, s.Unit, s.Price, s.EffectiveAt, variation, s.Currency, hasPrevious ? p : null));
            previous[key] = s.Price;
        }
        return history;
    }
    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct)
    {
        var current = await GetCurrentComparisonsAsync(ct); var history = await GetHistoryAsync(null, null, ct);
        return new(await db.Documents.CountAsync(d => d.Status == DocumentStatus.Approved, ct), await db.PriceSnapshots.Select(s => s.SupplierId).Distinct().CountAsync(ct),
         await db.Documents.CountAsync(d => d.Status == DocumentStatus.NeedsReview, ct), await db.PriceSnapshots.Select(s => s.CanonicalProductId).Distinct().CountAsync(ct),
         current.Where(c => c.SupplierCount >= 2).Take(5).Select(c => new TopPriceOpportunityDto(c.ProductName, c.BestSupplier, c.BestPrice, c.AveragePrice, c.SpreadPercentage, c.Currency, c.BaseUnit)).ToList(),
         history.Where(h => h.VariationPercentage.HasValue).OrderByDescending(h => Math.Abs(h.VariationPercentage!.Value)).Take(5)
         .Select(h => new PriceMoverDto(h.ProductName, h.SupplierName, h.PreviousPrice!.Value, h.Price, h.VariationPercentage!.Value, h.Currency, h.Unit)).ToList());
    }
}
public static class PriceMath
{
    public static decimal? CalculateVariation(decimal currentPrice, decimal previousPrice) => previousPrice == 0 ? null : Math.Round((currentPrice - previousPrice) / previousPrice * 100, 2);
    public static decimal CalculatePreviousPrice(decimal currentPrice, decimal variationPercentage) => variationPercentage == -100 ? 0 : Math.Round(currentPrice / (1 + variationPercentage / 100), 2);
}
