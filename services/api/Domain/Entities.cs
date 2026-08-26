namespace SistemasPrecios.Api.Domain;

public enum UserRole
{
    Admin = 1,
    Analyst = 2
}

public enum DocumentStatus
{
    Uploaded = 1,
    Processing = 2,
    Processed = 3,
    NeedsReview = 4,
    Approved = 5,
    Failed = 6
}

public enum JobStatus
{
    Queued = 1,
    Running = 2,
    Completed = 3,
    Failed = 4
}

public enum MatchStatus
{
    Suggested = 1,
    Approved = 2,
    Rejected = 3
}

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Supplier
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Document> Documents { get; set; } = [];
    public List<PriceSnapshot> PriceSnapshots { get; set; } = [];
}

public sealed class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    public string? FailureReason { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public List<ExtractedLine> ExtractedLines { get; set; } = [];
    public List<ProcessingJob> Jobs { get; set; } = [];
}

public sealed class ProcessingJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public string JobType { get; set; } = "extract";
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public sealed class ExtractedLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public int LineNumber { get; set; }
    public string RawText { get; set; } = string.Empty;
    public string? SuggestedName { get; set; }
    public string? SuggestedUnit { get; set; }
    public decimal? SuggestedQuantity { get; set; }
    public decimal? SuggestedPrice { get; set; }
    public decimal ConfidenceScore { get; set; }
    public bool NeedsReview { get; set; }
    public Guid? ApprovedCanonicalProductId { get; set; }
    public CanonicalProduct? ApprovedCanonicalProduct { get; set; }
    public string? ApprovedCanonicalProductName { get; set; }
    public decimal? ApprovedQuantity { get; set; }
    public string? ApprovedUnit { get; set; }
    public decimal? ApprovedPrice { get; set; }
    public List<ProductMatch> Matches { get; set; } = [];
}

public sealed class CanonicalProduct
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string BaseUnit { get; set; } = "UN";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ProductAlias> Aliases { get; set; } = [];
    public List<PriceSnapshot> PriceSnapshots { get; set; } = [];
}

public sealed class ProductAlias
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CanonicalProductId { get; set; }
    public CanonicalProduct CanonicalProduct { get; set; } = null!;
    public string Alias { get; set; } = string.Empty;
}

public sealed class ProductMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExtractedLineId { get; set; }
    public ExtractedLine ExtractedLine { get; set; } = null!;
    public Guid CanonicalProductId { get; set; }
    public CanonicalProduct CanonicalProduct { get; set; } = null!;
    public decimal ConfidenceScore { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Suggested;
}

public sealed class PriceSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public Guid CanonicalProductId { get; set; }
    public CanonicalProduct CanonicalProduct { get; set; } = null!;
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "UN";
    public DateTime EffectiveAt { get; set; } = DateTime.UtcNow;
}

public sealed class ComparisonResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CanonicalProductId { get; set; }
    public CanonicalProduct CanonicalProduct { get; set; } = null!;
    public Guid BestSupplierId { get; set; }
    public Supplier BestSupplier { get; set; } = null!;
    public decimal BestPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal HighestPrice { get; set; }
    public int SupplierCount { get; set; }
    public decimal PriceSpreadPercentage { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}
