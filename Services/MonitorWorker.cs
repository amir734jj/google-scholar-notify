using EfCoreRepository.Interfaces;
using ScholarNotify.Models;

namespace ScholarNotify.Services;

public sealed class MonitorWorker(
    IEfRepositoryCreator<Monitor> monitorCreator,
    MonitorService monitorService,
    IConfiguration configuration,
    ILogger<MonitorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = Math.Max(1, configuration.GetValue("MonitorWorker:ScanIntervalMinutes", 1));
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var monitors = await monitorCreator.CreateAsync();
            var due = (await monitors.NoTracking().GetAll())
                .Where(item => item.Enabled && item.NextCheckAt <= DateTimeOffset.UtcNow)
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