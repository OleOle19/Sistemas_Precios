namespace SistemasPrecios.Api.Services;

public static class UnitNormalizer
{
    public static (string Unit, decimal Quantity) Normalize(string unit, decimal quantity) => unit.Trim().ToUpperInvariant() switch
    {
        "G" => ("KG", quantity / 1000),
        "ML" => ("L", quantity / 1000),
        "KG" => ("KG", quantity),

        "L" => ("L", quantity),

        "UN" => ("UN", quantity),

        "M" => ("M", quantity),
        _ => throw new ArgumentException("Usa las unidades UN, KG, G, L, ML o M.")
    };
}
