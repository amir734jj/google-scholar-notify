using System.Collections.Concurrent;
using EfCoreRepository.Interfaces;
using ScholarNotify.Interfaces;
using ScholarNotify.Models;

namespace ScholarNotify.Services;

public sealed class MonitorService(
    IEfRepositoryCreator<Monitor> monitorCreator,
    IEfRepositoryCreator<Activity> activityCreator,
    ScholarService scholarService,
    SmsProxyHubService smsService) : IApplicationService
{
    private readonly ConcurrentDictionary<long, byte> _checking = new();

    public async Task<Monitor> CheckAsync(long id, CancellationToken cancellationToken = default)
    {
        if (!_checking.TryAdd(id, 0))
        {
            throw new InvalidOperationException("This profile is already being checked.");
        }

        await using var monitors = await monitorCreator.CreateAsync();
        var monitor = await monitors.Get(id) ?? throw new InvalidOperationException("Monitor not found.");

        try
        {
            var startedAt = DateTimeOffset.UtcNow;
            if (!NotificationWindow.IsOpen(monitor, startedAt))
            {
                var nextOpening = NotificationWindow.GetNextOpening(monitor, startedAt);
                await monitors.Update(id, entity => entity.NextCheckAt = nextOpening);
                await LogActivityAsync(id, "deferred", $"Check deferred until {nextOpening:O}.");
                return (await monitors.Get(id))!;
            }

            var snapshot = await scholarService.FetchProfileAsync(monitor.ScholarUrl, cancellationToken);
            var checkedAt = DateTimeOffset.UtcNow;
            var changed = snapshot.Citations != monitor.NotifiedCitations;
            var canNotify = !changed || NotificationWindow.IsOpen(monitor, checkedAt);
            var nextCheckAt = NotificationWindow.GetNextCheck(monitor, checkedAt);
            await monitors.Update(id, entity =>
            {
                entity.ScholarName = snapshot.Name;
                entity.CurrentCitations = snapshot.Citations;
                entity.LastCheckedAt = checkedAt;
                entity.NextCheckAt = nextCheckAt;
                entity.LastError = null;
            });
            if (changed && !canNotify)
            {
                await LogActivityAsync(
                    id,
                    "deferred",
                    $"SMS deferred until {nextCheckAt:O}; notifications are allowed from {monitor.NotificationStartHour:00}:00 to {monitor.NotificationEndHour:00} at UTC{monitor.UtcOffsetHours:+00;-00;+00}.");
            }
            else if (changed)
            {
                await smsService.SendCitationUpdateAsync(
                    monitor.PhoneNumber, snapshot.Name, monitor.NotifiedCitations, snapshot.Citations, id, cancellationToken);
                await monitors.Update(id, entity => entity.NotifiedCitations = snapshot.Citations);
                await LogActivityAsync(id, "notification", $"SMS sent: {monitor.NotifiedCitations:N0} to {snapshot.Citations:N0} citations.");
            }
            else
            {
                await LogActivityAsync(id, "check", $"Checked {snapshot.Name}: no change ({snapshot.Citations:N0}).");
            }
        }
        catch (Exception exception)
        {
            var failedAt = DateTimeOffset.UtcNow;
            await monitors.Update(id, entity =>
            {
                entity.LastError = exception.Message;
                entity.LastCheckedAt = failedAt;
                entity.NextCheckAt = NotificationWindow.GetNextCheck(monitor, failedAt);
            });
            await LogActivityAsync(id, "error", exception.Message);
            throw;
        }
        finally
        {
            _checking.TryRemove(id, out _);
        }

        return (await monitors.Get(id))!;
    }

    private async Task LogActivityAsync(long monitorId, string kind, string message)
    {
        await using var activities = await activityCreator.CreateAsync();
        await activities.Save(new Activity
        {
            MonitorId = monitorId,
            Kind = kind,
            Message = message,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}