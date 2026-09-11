using Newtonsoft.Json;

namespace ScholarNotify.Interfaces;

public sealed record SmsSendRequest(
    [property: JsonProperty("connectionId")] Guid? ConnectionId,
    [property: JsonProperty("phoneNumbers")] IReadOnlyList<string> PhoneNumbers,
    [property: JsonProperty("message")] string Message,
    [property: JsonProperty("payload")] string Payload);