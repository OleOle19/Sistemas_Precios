using SistemasPrecios.Api.Services;

namespace SistemasPrecios.Api.Tests;

public sealed class ProductMatchingServiceTests
{
    [Fact]
    public void Normalize_RemovesAccentsAndPunctuation()
    {
        var result = ProductMatchingService.Normalize("Azúcar Rubia 1KG");

        Assert.Equal("azucar rubia 1kg", result);
    }

    [Fact]
    public void ScoreCandidate_GivesStrongMatchForSimilarTokens()
    {
        var result = ProductMatchingService.ScoreCandidate(
            ProductMatchingService.Normalize("arroz extra 5kg"),
            ProductMatchingService.Normalize("arroz extra 5kg"));

        Assert.Equal(1m, result);
    }
}
