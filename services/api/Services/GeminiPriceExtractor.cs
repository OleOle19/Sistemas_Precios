using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SistemasPrecios.Api.Services;

public sealed class GeminiPriceExtractor(HttpClient http, IConfiguration configuration) : IPriceExtractor
{
    public async Task<IReadOnlyList<StructuredExtractionLine>> ExtractAsync(string path, string mime, string sourceKind, CancellationToken ct)
    {
        var key = PriceExtractionSettings.Key(configuration, "Gemini");
        if (string.IsNullOrWhiteSpace(key)) throw new ExtractionException("Configura la clave de Gemini y vuelve a procesar el archivo.");
        var model = PriceExtractionSettings.Model(configuration, "Gemini");
        if (!System.Text.RegularExpressions.Regex.IsMatch(model, "^gemini-[a-zA-Z0-9.-]+$"))
            throw new ExtractionException("El nombre del modelo de Gemini no es válido.");
        var payload = new JsonObject
        {
            ["systemInstruction"] = new JsonObject { ["parts"] = new JsonArray(new JsonObject { ["text"] = PriceExtractionContract.Instructions }) },
            ["contents"] = new JsonArray(new JsonObject
            {
                ["role"] = "user",
                ["parts"] = new JsonArray(
                    new JsonObject { ["text"] = $"Fuente declarada: {sourceKind}. Lee el archivo adjunto." },
                    new JsonObject { ["inlineData"] = new JsonObject { ["mimeType"] = mime, ["data"] = Convert.ToBase64String(await File.ReadAllBytesAsync(path, ct)) } })
            }),
            ["generationConfig"] = new JsonObject
            {
                ["candidateCount"] = 1,
                ["maxOutputTokens"] = 6000,
                ["responseMimeType"] = "application/json",
                ["responseJsonSchema"] = PriceExtractionContract.Schema()
            }
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent");
        request.Headers.Add("x-goog-api-key", key);
        request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) throw new ExtractionException(response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden => "Gemini rechazó el acceso. Verifica la clave y los permisos de tu proyecto de Google AI Studio.",
            System.Net.HttpStatusCode.TooManyRequests => "Gemini alcanzó un límite de solicitudes o cuota. Consulta los límites de tu proyecto en Google AI Studio y reintenta después; no es necesario activar facturación para seguir usando un nivel gratuito disponible.",
            System.Net.HttpStatusCode.NotFound => "El modelo de Gemini no está disponible para este proyecto. Revisa Gemini:Model y los modelos habilitados en Google AI Studio.",
            System.Net.HttpStatusCode.BadRequest => "Gemini rechazó el archivo o la configuración. Verifica la clave, el modelo y usa una foto o PDF más simple.",
            _ => $"Gemini no pudo leer el archivo (HTTP {(int)response.StatusCode}). Reintenta cuando el servicio esté disponible."
        });
        return ParseResponse(await response.Content.ReadAsStringAsync(ct));
    }

    public static IReadOnlyList<StructuredExtractionLine> ParseResponse(string json)
    {
        try
        {
            using var response = JsonDocument.Parse(json);
            var root = response.RootElement;
            if (root.TryGetProperty("promptFeedback", out var feedback) && feedback.TryGetProperty("blockReason", out var blocked) && blocked.GetString() is not (null or "BLOCK_REASON_UNSPECIFIED"))
                throw new ExtractionException("Gemini bloqueó la lectura de este archivo. Usa otro documento de prueba.");
            if (!root.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array || candidates.GetArrayLength() != 1)
                throw new ExtractionException("Gemini no devolvió una lectura completa del archivo.");
            var candidate = candidates[0];
            if (!candidate.TryGetProperty("finishReason", out var reason) || reason.GetString() != "STOP")
                throw new ExtractionException("La lectura de Gemini quedó incompleta o bloqueada. Divide el documento o usa una foto más clara.");
            var texts = candidate.GetProperty("content").GetProperty("parts").EnumerateArray()
                .Where(p => !(p.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True))
                .Where(p => p.TryGetProperty("text", out _)).Select(p => p.GetProperty("text").GetString());
            return PriceExtractionContract.ParseLines(string.Concat(texts));
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            throw new ExtractionException("Gemini devolvió una respuesta no válida. Revisa el archivo antes de reintentar.");
        }
    }
}
