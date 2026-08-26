using Hangfire;

namespace SistemasPrecios.Api.Services;

public interface IDocumentJobScheduler
{
    Task QueueProcessingAsync(Guid documentId, CancellationToken cancellationToken = default);
}

public sealed class HangfireDocumentJobScheduler(IBackgroundJobClient backgroundJobs) : IDocumentJobScheduler
{
    private readonly IBackgroundJobClient _backgroundJobs = backgroundJobs;

    public Task QueueProcessingAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        _backgroundJobs.Enqueue<IDocumentProcessingService>(service => service.ProcessDocumentAsync(documentId));
        return Task.CompletedTask;
    }
}

public sealed class InlineDocumentJobScheduler(
    IServiceScopeFactory scopeFactory,
    ILogger<InlineDocumentJobScheduler> logger) : IDocumentJobScheduler
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<InlineDocumentJobScheduler> _logger = logger;

    public Task QueueProcessingAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IDocumentProcessingService>();
                await service.ProcessDocumentAsync(documentId);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Fallo el procesamiento inline del documento {DocumentId}", documentId);
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }
}
