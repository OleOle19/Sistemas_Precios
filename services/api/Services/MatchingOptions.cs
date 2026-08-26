namespace SistemasPrecios.Api.Services;

public sealed class MatchingOptions
{
    public const string SectionName = "Matching";

    public decimal AutoApproveThreshold { get; set; } = 0.91m;
    public decimal ReviewThreshold { get; set; } = 0.76m;
}
