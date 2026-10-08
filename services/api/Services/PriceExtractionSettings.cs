namespace SistemasPrecios.Api.Services;

public static class PriceExtractionSettings
{
    public static string Provider(IConfiguration config) => (config["Extraction:Provider"] ?? "Gemini").Trim().ToLowerInvariant() switch
    {
        "gemini" => "Gemini",
        "openai" => "OpenAI",
        _ => throw new InvalidOperationException("Usa Extraction:Provider Gemini u OpenAI.")
    };

    public static string? Key(IConfiguration config, string provider)
    {
        var value = config[$"{provider}:ApiKey"];
        return !string.IsNullOrWhiteSpace(value) ? value : Environment.GetEnvironmentVariable(provider == "Gemini" ? "GEMINI_API_KEY" : "OPENAI_API_KEY");
    }

    public static string Model(IConfiguration config, string provider) => config[$"{provider}:Model"] ?? (provider == "Gemini" ? "gemini-3.1-flash-lite" : "gpt-5.4-mini");
}
