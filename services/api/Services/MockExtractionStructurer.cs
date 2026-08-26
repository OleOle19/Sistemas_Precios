using System.Globalization;
using System.Text.RegularExpressions;

namespace SistemasPrecios.Api.Services;

public interface IExtractionStructurer
{
    Task<IReadOnlyList<StructuredExtractionLine>> StructureAsync(
        IReadOnlyList<OcrTextBlock> blocks,
        CancellationToken cancellationToken);
}

public sealed partial class MockExtractionStructurer : IExtractionStructurer
{
    public Task<IReadOnlyList<StructuredExtractionLine>> StructureAsync(
        IReadOnlyList<OcrTextBlock> blocks,
        CancellationToken cancellationToken)
    {
        var lines = blocks
            .Where(block => !string.IsNullOrWhiteSpace(block.Text))
            .Select(block => BuildLine(block))
            .ToList();

        return Task.FromResult<IReadOnlyList<StructuredExtractionLine>>(lines);
    }

    private static StructuredExtractionLine BuildLine(OcrTextBlock block)
    {
        var price = ExtractPrice(block.Text);
        var quantity = ExtractQuantity(block.Text);
        var unit = ExtractUnit(block.Text);
        var name = ExtractName(block.Text);

        var confidence = price.HasValue
            ? Math.Max(0.55m, (decimal)block.Confidence)
            : 0.42m;

        return new StructuredExtractionLine(
            block.LineNumber,
            block.Text,
            name,
            unit,
            quantity,
            price,
            Math.Round(confidence, 2));
    }

    private static string ExtractName(string rawText)
    {
        var sanitized = PricePattern().Replace(rawText, string.Empty);
        sanitized = QuantityPattern().Replace(sanitized, string.Empty);
        sanitized = sanitized.Replace("S/", string.Empty, StringComparison.OrdinalIgnoreCase);
        return Regex.Replace(sanitized, @"\s+", " ").Trim();
    }

    private static decimal? ExtractPrice(string rawText)
    {
        var match = PricePattern().Matches(rawText).LastOrDefault();
        if (match is null)
        {
            return null;
        }

        var value = match.Value.Replace("S/", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        value = value.Replace(",", ".");

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var price)
            ? price
            : null;
    }

    private static decimal? ExtractQuantity(string rawText)
    {
        var match = QuantityPattern().Match(rawText);
        if (!match.Success)
        {
            return 1;
        }

        var value = match.Groups["quantity"].Value.Replace(",", ".");
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity)
            ? quantity
            : 1;
    }

    private static string ExtractUnit(string rawText)
    {
        var match = QuantityPattern().Match(rawText);
        if (!match.Success)
        {
            return "UN";
        }

        return match.Groups["unit"].Value.ToUpperInvariant();
    }

    [GeneratedRegex(@"(S\/\s*)?\d+([.,]\d{1,2})?")]
    private static partial Regex PricePattern();

    [GeneratedRegex(@"(?<quantity>\d+([.,]\d+)?)\s*(?<unit>KG|KGS|G|GR|L|LT|LTS|ML|UN)", RegexOptions.IgnoreCase)]
    private static partial Regex QuantityPattern();
}
