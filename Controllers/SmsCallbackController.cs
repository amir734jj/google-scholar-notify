using EfCoreRepository.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ScholarNotify.Interfaces;
using ScholarNotify.Models;
using ScholarNotify.Services;

namespace ScholarNotify.Controllers;

[ApiController]
[Route("api/sms-callback")]
public sealed class SmsCallbackController(
    IEfRepositoryCreator<Monitor> monitorCreator,
    IEfRepositoryCreator<Activity> activityCreator,
    IConfiguration configuration,
    ILogger<SmsCallbackController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(SmsWebhookCallback callback)
    {
        if (Guid.TryParse(configuration["SMS_CONNECTION_ID"] ?? configuration["SmsProxyHub:ConnectionId"], out var connectionId)
            && callback.ConnectionId != connectionId)
        {
            return Unauthorized();
        }

        SmsCallbackCorrelation? correlation;
        try
        {
            correlation = JsonConvert.DeserializeObject<SmsCallbackCorrelation>(callback.OriginalPayload ?? string.Empty);
        }
        catch (JsonException)
        {
            return Ok();
        }

        if (correlation is null || correlation.Source != "google-scholar-notify")
        {
            return Ok();
        }

        await using var monitors = await monitorCreator.CreateAsync();
        var monitor = await monitors.Get(correlation.MonitorId);
        if (monitor is null)
        {
            logger.LogWarning("Ignoring SMS callback for missing monitor {MonitorId}.", correlation.MonitorId);
            return Ok();
        }

        var eventName = callback.Event.ToLowerInvariant();
        switch (eventName)
        {
            case "smsfailed":
                await monitors.Update(monitor.Id, entity =>
                {
                    if (correlation.PreviousCitations is { } previousCitations
                        && entity.NotifiedCitations == correlation.Citations)
                    {
                        entity.NotifiedCitations = previousCitations;
                    }

                    entity.LastError = callback.Reason ?? "SMS delivery failed.";
                    entity.NextCheckAt = NotificationWindow.GetNextCheck(entity, DateTimeOffset.UtcNow);
                });
                await LogActivityAsync(monitor.Id, "sms-failed", callback.Reason ?? "SMS delivery failed.");
                break;
            case "smsdelivered":
                await monitors.Update(monitor.Id, entity =>
                {
                    entity.NotifiedCitations = correlation.Citations;
                    entity.LastError = null;
                });
                await LogActivityAsync(monitor.Id, "sms-delivered", $"SMS delivered to {callback.Phone}.");
                break;
            case "smssent":
                await LogActivityAsync(monitor.Id, "sms-sent", $"SMS accepted for {callback.Phone}.");
                break;
            case "smsreply":
                await LogActivityAsync(monitor.Id, "sms-reply", $"Reply from {callback.Phone}: {callback.Message ?? "(empty)"}");
                break;
            default:
                logger.LogWarning("Ignoring unknown SMS callback event {Event}.", callback.Event);
                break;
        }

        return Ok();
    }

    private async Task LogActivityAsync(long monitorId, string kind, string message)
    {
        var boundedMessage = message[..Math.Min(message.Length, 4000)];
        await using var activities = await activityCreator.CreateAsync();
        await activities.Save(new Activity
        {
            MonitorId = monitorId,
            Kind = kind,
            Message = boundedMessage,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}