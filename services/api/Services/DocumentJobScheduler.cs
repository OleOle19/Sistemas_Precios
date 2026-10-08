using Microsoft.EntityFrameworkCore;
using SistemasPrecios.Api.Data;
using SistemasPrecios.Api.Domain;
namespace SistemasPrecios.Api.Services;
public interface IDocumentJobScheduler { Task QueueProcessingAsync(Guid documentId, CancellationToken cancellationToken = default); }
// Uploaded documents are the durable queue. Deploy one API worker instance.
public sealed class DocumentJobScheduler : IDocumentJobScheduler
{ public Task QueueProcessingAsync(Guid documentId, CancellationToken cancellationToken = default) => Task.CompletedTask; }
public sealed class DocumentWorker(IServiceScopeFactory scopes, ILogger<DocumentWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        await using (var scope = scopes.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            foreach (var d in await db.Documents.Where(d => d.Status == DocumentStatus.Processing).ToListAsync(token))
            { d.Status = DocumentStatus.Failed; d.FailureReason = "La lectura se interrumpió al reiniciar el servicio. Reintenta cuando estés listo."; }
            foreach (var j in await db.ProcessingJobs.Where(j => j.Status == JobStatus.Running).ToListAsync(token))
            { j.Status = JobStatus.Failed; j.ErrorMessage = "Lectura interrumpida."; j.CompletedAt = DateTime.UtcNow; }
            await db.SaveChangesAsync(token);
        }
        while (!token.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var id = await db.Documents.Where(d => d.Status == DocumentStatus.Uploaded).OrderBy(d => d.UploadedAt).Select(d => (Guid?)d.Id).FirstOrDefaultAsync(token);
                if (id.HasValue) await scope.ServiceProvider.GetRequiredService<IDocumentProcessingService>().ProcessDocumentAsync(id.Value);
                else await Task.Delay(1500, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError("Falló el lector: {ErrorType}", ex.GetType().Name); await Task.Delay(5000, token); }
        }
    }
}
