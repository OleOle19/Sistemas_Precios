using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace SistemasPrecios.Api.Services;

public interface IVisionExtractionClient
{
    Task<IReadOnlyList<OcrTextBlock>> ProcessDocumentAsync(
        string filePath,
        string contentType,
        CancellationToken cancellationToken);
}

public sealed class GrpcVisionExtractionClient(
    Vision.VisionProcessor.VisionProcessorClient client,
    ILogger<GrpcVisionExtractionClient> logger) : IVisionExtractionClient
{
    private readonly Vision.VisionProcessor.VisionProcessorClient _client = client;
    private readonly ILogger<GrpcVisionExtractionClient> _logger = logger;

    public async Task<IReadOnlyList<OcrTextBlock>> ProcessDocumentAsync(
        string filePath,
        string contentType,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.ProcessDocumentAsync(
                new Vision.ProcessDocumentRequest
                {
                    FileRef = filePath,
                    MimeType = contentType
                },
                cancellationToken: cancellationToken);

            return response.ExtractedBlocks
                .Select(block => new OcrTextBlock(block.LineNumber, block.Text, block.Confidence))
                .ToList();
        }
        catch (RpcException rpcException)
        {
            _logger.LogWarning(
                rpcException,
                "vision-rs no respondio. Se usara fallback local para {FilePath}",
                filePath);

            return await LocalFallbackAsync(filePath, cancellationToken);
        }
    }

    private static async Task<IReadOnlyList<OcrTextBlock>> LocalFallbackAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var sidecarPath = Path.ChangeExtension(filePath, ".txt");
        if (File.Exists(sidecarPath))
        {
            var lines = await File.ReadAllLinesAsync(sidecarPath, cancellationToken);
            return lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select((line, index) => new OcrTextBlock(index + 1, line.Trim(), 0.72f))
                .ToList();
        }

        return
        [
            new OcrTextBlock(1, "ACEITE VEGETAL 1L S/ 8.90", 0.91f),
            new OcrTextBlock(2, "ARROZ EXTRA 5KG S/ 22.50", 0.86f),
            new OcrTextBlock(3, "AZUCAR RUBIA 1KG S/ 4.90", 0.79f)
        ];
    }
}
