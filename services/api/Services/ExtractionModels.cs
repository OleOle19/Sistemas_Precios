namespace SistemasPrecios.Api.Services;

public sealed record OcrTextBlock(int LineNumber, string Text, float Confidence);

public sealed record StructuredExtractionLine(
    int LineNumber,
    string RawText,
    string? SuggestedName,
    string? SuggestedUnit,
    decimal? SuggestedQuantity,
    decimal? SuggestedPrice,
    decimal ConfidenceScore, string? SuggestedCurrency = null);

public sealed record ProductMatchCandidate(
    Guid CanonicalProductId,
    string CanonicalProductName,
    decimal ConfidenceScore, string? SuggestedCurrency = null);
