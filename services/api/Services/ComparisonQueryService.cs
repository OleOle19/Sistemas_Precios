using Microsoft.EntityFrameworkCore;
using SistemasPrecios.Api.Data;
using SistemasPrecios.Api.Domain;
using SistemasPrecios.Api.Dtos;

namespace SistemasPrecios.Api.Services;

public interface IComparisonQueryService
{
    Task<IReadOnlyList<CurrentComparisonItemDto>> GetCurrentComparisonsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ComparisonHistoryItemDto>> GetHistoryAsync(Guid? productId, Guid? supplierId, CancellationToken cancellationToken);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken);
    Task RefreshAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken);
}

public sealed class ComparisonQueryService(ApplicationDbContext db) : IComparisonQueryService
{
    private readonly ApplicationDbContext _db = db;

    public async Task<IReadOnlyList<CurrentComparisonItemDto>> GetCurrentComparisonsAsync(CancellationToken cancellationToken)
    {
        return await _db.ComparisonResults
            .Include(result => result.CanonicalProduct)
            .Include(result => result.BestSupplier)
            .OrderByDescending(result => result.PriceSpreadPercentage)
            .Select(result => new CurrentComparisonItemDto(
                result.CanonicalProductId,
                result.CanonicalProduct.Name,
                result.CanonicalProduct.BaseUnit,
                result.BestSupplier.Name,
                result.BestPrice,
                result.AveragePrice,
                result.HighestPrice,
                result.PriceSpreadPercentage,
                result.CalculatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ComparisonHistoryItemDto>> GetHistoryAsync(
        Guid? productId,
        Guid? supplierId,
        CancellationToken cancellationToken)
    {
        var query = _db.PriceSnapshots
            .Include(snapshot => snapshot.CanonicalProduct)
            .Include(snapshot => snapshot.Supplier)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(snapshot => snapshot.CanonicalProductId == productId.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(snapshot => snapshot.SupplierId == supplierId.Value);
        }

        var items = await query
            .OrderBy(snapshot => snapshot.CanonicalProduct.Name)
            .ThenBy(snapshot => snapshot.Supplier.Name)
            .ThenBy(snapshot => snapshot.EffectiveAt)
            .Select(snapshot => new
            {
                snapshot.CanonicalProduct.Name,
                SupplierName = snapshot.Supplier.Name,
                snapshot.Unit,
                snapshot.Price,
                snapshot.EffectiveAt
            })
            .ToListAsync(cancellationToken);

        var history = new List<ComparisonHistoryItemDto>(items.Count);
        var lastPriceByKey = new Dictionary<string, decimal>();

        foreach (var item in items)
        {
            var key = $"{item.Name}:{item.SupplierName}";
            var variation = lastPriceByKey.TryGetValue(key, out var previousPrice)
                ? PriceMath.CalculateVariation(item.Price, previousPrice)
                : null;

            history.Add(new ComparisonHistoryItemDto(
                item.Name,
                item.SupplierName,
                item.Unit,
                item.Price,
                item.EffectiveAt,
                variation));

            lastPriceByKey[key] = item.Price;
        }

        return history;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken)
    {
        var bestPrices = await _db.ComparisonResults
            .Include(result => result.CanonicalProduct)
            .Include(result => result.BestSupplier)
            .OrderByDescending(result => result.PriceSpreadPercentage)
            .Take(5)
            .Select(result => new TopPriceOpportunityDto(
                result.CanonicalProduct.Name,
                result.BestSupplier.Name,
                result.BestPrice,
                result.AveragePrice,
                result.PriceSpreadPercentage))
            .ToListAsync(cancellationToken);

        var history = await GetHistoryAsync(null, null, cancellationToken);
        var biggestMovers = history
            .Where(item => item.VariationPercentage.HasValue)
            .OrderByDescending(item => Math.Abs(item.VariationPercentage!.Value))
            .Take(5)
            .Select(item => new PriceMoverDto(
                item.ProductName,
                item.SupplierName,
                PriceMath.CalculatePreviousPrice(item.Price, item.VariationPercentage ?? 0),
                item.Price,
                item.VariationPercentage ?? 0))
            .ToList();

        return new DashboardSummaryDto(
            DocumentsProcessed: await _db.Documents.CountAsync(cancellationToken),
            SuppliersCompared: await _db.PriceSnapshots.Select(snapshot => snapshot.SupplierId).Distinct().CountAsync(cancellationToken),
            PendingReviews: await _db.Documents.CountAsync(document => document.Status == DocumentStatus.NeedsReview, cancellationToken),
            ProductsTracked: await _db.CanonicalProducts.CountAsync(cancellationToken),
            BestPrices: bestPrices,
            BiggestMovers: biggestMovers);
    }

    public async Task RefreshAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken)
    {
        var normalizedIds = productIds.Distinct().ToArray();
        if (normalizedIds.Length == 0)
        {
            return;
        }

        var snapshots = await _db.PriceSnapshots
            .Include(snapshot => snapshot.Supplier)
            .Include(snapshot => snapshot.CanonicalProduct)
            .Where(snapshot => normalizedIds.Contains(snapshot.CanonicalProductId))
            .OrderBy(snapshot => snapshot.EffectiveAt)
            .ToListAsync(cancellationToken);

        foreach (var productId in normalizedIds)
        {
            var productSnapshots = snapshots
                .Where(snapshot => snapshot.CanonicalProductId == productId)
                .GroupBy(snapshot => snapshot.SupplierId)
                .Select(group => group.OrderByDescending(item => item.EffectiveAt).First())
                .ToList();

            if (productSnapshots.Count == 0)
            {
                continue;
            }

            var best = productSnapshots.OrderBy(snapshot => snapshot.Price).First();
            var average = productSnapshots.Average(snapshot => snapshot.Price);
            var highest = productSnapshots.Max(snapshot => snapshot.Price);
            var spread = average == 0 ? 0 : Math.Round(((highest - best.Price) / average) * 100, 2);

            var comparison = await _db.ComparisonResults
                .FirstOrDefaultAsync(result => result.CanonicalProductId == productId, cancellationToken);

            if (comparison is null)
            {
                comparison = new ComparisonResult
                {
                    CanonicalProductId = productId
                };
                _db.ComparisonResults.Add(comparison);
            }

            comparison.BestSupplierId = best.SupplierId;
            comparison.BestPrice = best.Price;
            comparison.AveragePrice = Math.Round(average, 2);
            comparison.HighestPrice = highest;
            comparison.SupplierCount = productSnapshots.Count;
            comparison.PriceSpreadPercentage = spread;
            comparison.CalculatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}

public static class PriceMath
{
    public static decimal? CalculateVariation(decimal currentPrice, decimal previousPrice)
    {
        if (previousPrice == 0)
        {
            return null;
        }

        return Math.Round(((currentPrice - previousPrice) / previousPrice) * 100, 2);
    }

    public static decimal CalculatePreviousPrice(decimal currentPrice, decimal variationPercentage)
    {
        if (variationPercentage == -100)
        {
            return 0;
        }

        var divisor = 1 + (variationPercentage / 100);
        if (divisor == 0)
        {
            return 0;
        }

        return Math.Round(currentPrice / divisor, 2);
    }
}
