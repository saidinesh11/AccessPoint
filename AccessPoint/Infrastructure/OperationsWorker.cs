using Microsoft.EntityFrameworkCore;
using App.Data;
using App.Domain;
using App.Services;
namespace App.Infrastructure;
/// <summary>Local delivery adapter. Does not send emails. A single worker is supported.</summary>
public sealed class OperationsWorker(IServiceScopeFactory scopes, ILogger<OperationsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var workflow = scope.ServiceProvider.GetRequiredService<WorkflowService>();
                await workflow.RunMaintenance(stoppingToken);
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var messages = await db.Outbox.Where(m => m.ProcessedUtc == null).OrderBy(m => m.CreatedUtc).Take(20).ToListAsync(stoppingToken);
                foreach (var message in messages)
                {
                    // Durable local sink and processed marker commit together. A crash rolls both back.
                    if (!await db.Deliveries.AnyAsync(d => d.Id == message.Id, stoppingToken))
                        db.Deliveries.Add(new DeliveryRecord { Id = message.Id, Kind = message.Kind, DeliveredUtc = DateTime.UtcNow });
                    message.ProcessedUtc = DateTime.UtcNow;
                }
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception error) { logger.LogWarning(error, "Operations cycle failed; pending messages will be retried next cycle"); }
        }
    }
}
