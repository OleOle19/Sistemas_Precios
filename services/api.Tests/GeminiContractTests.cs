using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SistemasPrecios.Api.Services;

namespace SistemasPrecios.Api.Tests;

public sealed class GeminiContractTests
{
    private const string Lines = """
        {"lines":[{"rawText":"Hilo 500g S/ 15","name":"Hilo","unit":"G","quantity":500,"price":15,"currency":"PEN"}]}
        """;
    private static string Response(string reason = "STOP", string? lines = null) => JsonSerializer.Serialize(new
    {
        candidates = new[] { new { finishReason = reason, content = new { parts = new object[] { new { text = "No es el resultado", thought = true }, new { text = lines ?? Lines } } } } }
    });
    [Fact]
    public void CompletedResponseReadsPricesAndIgnoresThoughtParts()
    {
        var line = GeminiPriceExtractor.ParseResponse(Response()).Single();
        Assert.Equal(500, line.SuggestedQuantity); Assert.Equal(15, line.SuggestedPrice); Assert.Equal("G", line.SuggestedUnit); Assert.Equal("PEN", line.SuggestedCurrency);
    }
    [Theory]
    [InlineData("MAX_TOKENS")][InlineData("SAFETY")][InlineData("OTHER")]
    public void PartialOrBlockedResponseCannotCreatePrices(string reason) => Assert.Throws<ExtractionException>(() => GeminiPriceExtractor.ParseResponse(Response(reason)));
    [Theory]
    [InlineData("{}")][InlineData("{\"promptFeedback\":{\"blockReason\":\"SAFETY\"}}")][InlineData("not json")]
    public void MissingOrMalformedCandidateIsExplicitFailure(string json) => Assert.Throws<ExtractionException>(() => GeminiPriceExtractor.ParseResponse(json));
    [Fact]
    public void MissingFieldsCannotBecomeInventedValues() => Assert.Throws<ExtractionException>(() => GeminiPriceExtractor.ParseResponse(Response(lines: "{\"lines\":[{\"name\":\"Hilo\"}]}")));
    [Theory]
    [InlineData("image/png")][InlineData("application/pdf")]
    public async Task FileBytesAndMimeAreSentWithKeyInHeaderOnly(string mime)
    {
        var file = Path.GetTempFileName(); await File.WriteAllBytesAsync(file, [1,2,3,4]);
        try
        {
            var handler = new CaptureHandler(HttpStatusCode.OK); var extractor = new GeminiPriceExtractor(new HttpClient(handler), Configuration());
            await extractor.ExtractAsync(file, mime, "quotation", default);
            Assert.Equal("test-only-gemini-key", handler.Key); Assert.DoesNotContain("key", handler.Url!);
            using var body = JsonDocument.Parse(handler.Body!);
            var attachment = body.RootElement.GetProperty("contents")[0].GetProperty("parts")[1].GetProperty("inlineData");
            Assert.Equal("AQIDBA==", attachment.GetProperty("data").GetString()); Assert.Equal(mime, attachment.GetProperty("mimeType").GetString());
            Assert.Equal("application/json", body.RootElement.GetProperty("generationConfig").GetProperty("responseMimeType").GetString());
            Assert.False(body.RootElement.GetProperty("generationConfig").GetProperty("responseJsonSchema").GetProperty("additionalProperties").GetBoolean());
        }
        finally { File.Delete(file); }
    }
    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)][InlineData(HttpStatusCode.Forbidden)][InlineData(HttpStatusCode.BadRequest)]
    public async Task ProviderErrorsNeverExposeResponseBodyOrFallbackPrices(HttpStatusCode status)
    {
        var file = Path.GetTempFileName();
        try
        {
            var handler = new CaptureHandler(status); var extractor = new GeminiPriceExtractor(new HttpClient(handler), Configuration());
            var error = await Assert.ThrowsAsync<ExtractionException>(() => extractor.ExtractAsync(file, "image/png", "label", default));
            Assert.DoesNotContain("private-provider-body", error.Message); Assert.Contains("Gemini", error.Message);
        }
        finally { File.Delete(file); }
    }
    [Fact]
    public void ProviderKeyIsNeverBorrowedFromOtherProvider()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Gemini:ApiKey"]="gemini-test", ["OpenAI:ApiKey"]="openai-test", ["Extraction:Provider"]="Gemini" }).Build();
        Assert.Equal("Gemini", PriceExtractionSettings.Provider(config)); Assert.Equal("gemini-test", PriceExtractionSettings.Key(config, PriceExtractionSettings.Provider(config)));
        config["Extraction:Provider"]="OpenAI";
        Assert.Equal("openai-test", PriceExtractionSettings.Key(config, PriceExtractionSettings.Provider(config)));
        config["Extraction:Provider"]="unexpected";
        Assert.Throws<InvalidOperationException>(()=>PriceExtractionSettings.Provider(config));
    }
    private static IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Gemini:ApiKey"]="test-only-gemini-key" }).Build();
    private sealed class CaptureHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public string? Body, Key, Url;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Body=await request.Content!.ReadAsStringAsync(ct); Key=request.Headers.GetValues("x-goog-api-key").Single(); Url=request.RequestUri!.ToString();
            return new(status) { Content=new StringContent(status==HttpStatusCode.OK ? Response() : "private-provider-body", Encoding.UTF8,"application/json") };
        }
    }
}
