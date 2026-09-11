using EfCoreRepository.Interfaces;
using ScholarNotify.Models;

namespace ScholarNotify.Services;

public sealed class MonitorWorker(
    IEfRepositoryCreator<Monitor> monitorCreator,
    MonitorService monitorService,
    ILogger<MonitorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(NotificationWindow.CheckInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var monitors = await monitorCreator.CreateAsync();
            var now = DateTimeOffset.UtcNow;
            var due = (await monitors.NoTracking().GetAll())
                .Where(item => item.NextCheckAt <= now
                    && NotificationWindow.IsOpen(item, now))
                .OrderBy(item => item.NextCheckAt)
                .Take(10);
            foreach (var id in due.Select(item => item.Id))
            {
                try
                {
                    await monitorService.CheckAsync(id, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Monitor {MonitorId} failed", id);
                }
            }
        }
    }
}