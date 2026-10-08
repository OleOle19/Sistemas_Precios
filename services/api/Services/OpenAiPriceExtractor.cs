using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SistemasPrecios.Api.Services;

public interface IPriceExtractor
{
    Task<IReadOnlyList<StructuredExtractionLine>> ExtractAsync(string path, string mime, string sourceKind, CancellationToken ct);
}

public sealed class OpenAiPriceExtractor(HttpClient http, IConfiguration configuration) : IPriceExtractor
{
    public async Task<IReadOnlyList<StructuredExtractionLine>> ExtractAsync(string path, string mime, string sourceKind, CancellationToken ct)
    {
        var key = PriceExtractionSettings.Key(configuration, "OpenAI");
        if (string.IsNullOrWhiteSpace(key))
            throw new ExtractionException("Configura la clave de OpenAI y vuelve a procesar el archivo.");
        var bytes = await File.ReadAllBytesAsync(path, ct);
        var data = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
        var attachment = mime == "application/pdf"
            ? new JsonObject { ["type"] = "input_file", ["filename"] = "document.pdf", ["file_data"] = data }
            : new JsonObject { ["type"] = "input_image", ["image_url"] = data, ["detail"] = "high" };
        var schema = PriceExtractionContract.Schema();
        var payload = new JsonObject
        {
            ["model"] = PriceExtractionSettings.Model(configuration, "OpenAI"),
            ["store"] = false,
            ["max_output_tokens"] = 6000,
            ["instructions"] = PriceExtractionContract.Instructions,
            ["input"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = new JsonArray(new JsonObject { ["type"] = "input_text", ["text"] = $"Fuente declarada: {sourceKind}. Lee el archivo adjunto." }, attachment) }),
            ["text"] = new JsonObject { ["format"] = new JsonObject { ["type"] = "json_schema", ["name"] = "price_lines", ["strict"] = true, ["schema"] = schema } }
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            string? code = null;
            try
            {
                using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                if (error.RootElement.TryGetProperty("error", out var detail) && detail.TryGetProperty("code", out var errorCode))
                    code = errorCode.GetString();
            }
            catch (JsonException) { }
            throw new ExtractionException(code == "insufficient_quota" ? "OpenAI indica cuota insuficiente. Revisa el saldo y la facturación de tu proyecto de API antes de reintentar." : response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => "La clave de OpenAI no es válida.",
                System.Net.HttpStatusCode.TooManyRequests => "OpenAI limitó la solicitud. Revisa saldo o cuota antes de reintentar.",
                _ => $"OpenAI no pudo leer el archivo (HTTP {(int)response.StatusCode}). Intenta con una foto más clara o revisa la configuración."
            });
        }
        return ParseResponse(await response.Content.ReadAsStringAsync(ct));
    }

    public static IReadOnlyList<StructuredExtractionLine> ParseResponse(string json)
    {
        using var response = JsonDocument.Parse(json);
        if (!response.RootElement.TryGetProperty("status", out var status) || status.GetString() != "completed")
            throw new ExtractionException("La lectura quedó incompleta. Divide el documento o usa una foto más clara.");
        var texts = response.RootElement.GetProperty("output").EnumerateArray()
            .Where(o => o.TryGetProperty("type", out var type) && type.GetString() == "message")
            .SelectMany(o => o.GetProperty("content").EnumerateArray())
            .Where(c => c.GetProperty("type").GetString() == "output_text")
            .Select(c => c.GetProperty("text").GetString());
        return PriceExtractionContract.ParseLines(string.Concat(texts));
    }
}

public sealed class ExtractionException(string message) : Exception(message);
