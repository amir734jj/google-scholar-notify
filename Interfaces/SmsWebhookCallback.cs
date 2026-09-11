using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ScholarNotify.Interfaces;

public sealed record SmsWebhookCallback(
    [property: JsonProperty("event")] string Event,
    [property: JsonProperty("phone")] string Phone,
    [property: JsonProperty("message")] string? Message,
    [property: JsonProperty("subject")] string? Subject,
    [property: JsonProperty("attachments")] JToken? Attachments,
    [property: JsonProperty("originalPayload")] string? OriginalPayload,
    [property: JsonProperty("connectionId")] Guid ConnectionId,
    [property: JsonProperty("reason")] string? Reason,
    [property: JsonProperty("timestamp")] DateTimeOffset Timestamp);

public sealed record SmsCallbackCorrelation(
    [property: JsonProperty("source")] string Source,
    [property: JsonProperty("monitorId")] long MonitorId,
    [property: JsonProperty("previousCitations")] int? PreviousCitations,
    [property: JsonProperty("citations")] int Citations);
