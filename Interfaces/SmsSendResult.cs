using Newtonsoft.Json;

namespace ScholarNotify.Interfaces;

public sealed record SmsSendResult(
    [property: JsonProperty("status")] string? Status);