namespace SistemasPrecios.Api.Dtos;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoggedInUserResponse(Guid Id, string FullName, string Email, string Role);
public sealed record SessionResponse(Guid Id, string FullName, string Email, string Role, string WorkspaceName);
public sealed record TeamUserResponse(Guid Id, string FullName, string Email, string Role);
public sealed record CreateTeamUserRequest(string FullName, string Email, string Password, string Role);
public sealed record UpdateTeamRoleRequest(string Role);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record SupplierCreateRequest(string Name, string? ContactEmail);

public sealed record SupplierResponse(Guid Id, string Name, string? ContactEmail, int DocumentCount);

public sealed record DocumentSummaryResponse(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    string FileName,
    string ContentType,
    string Status,
    DateTime UploadedAt,
    int ExtractedLineCount,
    string? FailureReason);

public sealed record ProductMatchResponse(
    Guid Id,
    Guid CanonicalProductId,
    string CanonicalProductName,
    decimal ConfidenceScore,
    string Status);

public sealed record ExtractedLineResponse(
    Guid Id,
    int LineNumber,
    string RawText,
    string? SuggestedName,
    string? SuggestedUnit,
    decimal? SuggestedQuantity,
    decimal? SuggestedPrice,
    decimal ConfidenceScore,
    bool NeedsReview,
    Guid? ApprovedCanonicalProductId,
    string? ApprovedCanonicalProductName,
    decimal? ApprovedQuantity,
    string? ApprovedUnit,
    decimal? ApprovedPrice,
    IReadOnlyList<ProductMatchResponse> Matches, string? SuggestedCurrency, string? ApprovedCurrency, bool Excluded);

public sealed record DocumentDetailResponse(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    string FileName,
    string ContentType,
    string Status,
    DateTime UploadedAt,
    int ExtractedLineCount,
    string? FailureReason,
    IReadOnlyList<ExtractedLineResponse> Lines, string SourceKind, DateTime ObservedAt);

public sealed record ReviewLineRequest(
    Guid ExtractedLineId,
    string CanonicalName,
    string Unit,
    decimal Quantity,
    decimal Price, string Currency = "PEN", bool Excluded = false);

public sealed record DocumentReviewRequest(IReadOnlyList<ReviewLineRequest> Lines);

public sealed record CurrentComparisonItemDto(
    Guid ProductId,
    string ProductName,
    string BaseUnit,
    string BestSupplier,
    decimal BestPrice,
    decimal AveragePrice,
    decimal HighestPrice,
    decimal SpreadPercentage,
    DateTime CalculatedAt, string Currency, int SupplierCount);

public sealed record ComparisonHistoryItemDto(
    string ProductName,
    string SupplierName,
    string Unit,
    decimal Price,
    DateTime EffectiveAt,
    decimal? VariationPercentage, string Currency, decimal? PreviousPrice);

public sealed record TopPriceOpportunityDto(
    string ProductName,
    string SupplierName,
    decimal BestPrice,
    decimal AveragePrice,
    decimal SpreadPercentage, string Currency, string Unit);

public sealed record PriceMoverDto(
    string ProductName,
    string SupplierName,
    decimal PreviousPrice,
    decimal CurrentPrice,
    decimal VariationPercentage, string Currency, string Unit);

public sealed record DashboardSummaryDto(
    int DocumentsProcessed,
    int SuppliersCompared,
    int PendingReviews,
    int ProductsTracked,
    IReadOnlyList<TopPriceOpportunityDto> BestPrices,
    IReadOnlyList<PriceMoverDto> BiggestMovers);
