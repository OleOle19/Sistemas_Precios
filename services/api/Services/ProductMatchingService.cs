using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SistemasPrecios.Api.Data;

namespace SistemasPrecios.Api.Services;

public interface IProductMatchingService
{
    Task<IReadOnlyList<ProductMatchCandidate>> FindCandidatesAsync(
        StructuredExtractionLine line,
        CancellationToken cancellationToken);
}

public sealed class ProductMatchingService(
    ApplicationDbContext db,
    IOptions<MatchingOptions> matchingOptions) : IProductMatchingService
{
    private readonly ApplicationDbContext _db = db;
    private readonly MatchingOptions _matchingOptions = matchingOptions.Value;

    public async Task<IReadOnlyList<ProductMatchCandidate>> FindCandidatesAsync(
        StructuredExtractionLine line,
        CancellationToken cancellationToken)
    {
        var products = await _db.CanonicalProducts
            .Include(product => product.Aliases)
            .ToListAsync(cancellationToken);

        var normalizedLine = Normalize(line.SuggestedName ?? line.RawText);

        var candidates = products
            .Select(product =>
            {
                var names = product.Aliases.Select(alias => alias.Alias).Append(product.Name);
                var score = names.Max(alias => ScoreCandidate(normalizedLine, Normalize(alias)));
                return new ProductMatchCandidate(product.Id, product.Name, score);
            })
            .Where(candidate => candidate.ConfidenceScore >= _matchingOptions.ReviewThreshold - 0.18m)
            .OrderByDescending(candidate => candidate.ConfidenceScore)
            .Take(3)
            .ToList();

        return candidates;
    }

    public static decimal ScoreCandidate(string source, string candidate)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(candidate))
        {
            return 0m;
        }

        if (source == candidate)
        {
            return 1m;
        }

        var sourceTokens = source.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var candidateTokens = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var intersection = sourceTokens.Intersect(candidateTokens).Count();
        var union = sourceTokens.Union(candidateTokens).Count();
        var tokenScore = union == 0 ? 0m : (decimal)intersection / union;

        if (candidate.Contains(source, StringComparison.Ordinal) || source.Contains(candidate, StringComparison.Ordinal))
        {
            tokenScore = Math.Max(tokenScore, 0.88m);
        }

        return Math.Round(tokenScore, 2);
    }

    public static string Normalize(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark &&
                (char.IsLetterOrDigit(character) || char.IsWhiteSpace(character)))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return string.Join(
            ' ',
            builder.ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
