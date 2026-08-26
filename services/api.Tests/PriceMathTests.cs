using SistemasPrecios.Api.Services;

namespace SistemasPrecios.Api.Tests;

public sealed class PriceMathTests
{
    [Fact]
    public void CalculateVariation_ReturnsExpectedPercentage()
    {
        var result = PriceMath.CalculateVariation(4.90m, 4.30m);

        Assert.Equal(13.95m, result);
    }

    [Fact]
    public void CalculatePreviousPrice_RebuildsOriginalValue()
    {
        var result = PriceMath.CalculatePreviousPrice(4.90m, 13.95m);

        Assert.Equal(4.30m, result);
    }
}
