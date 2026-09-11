using Newtonsoft.Json;

namespace ScholarNotify.Interfaces;

public sealed record SmsSendResponse(
    [property: JsonProperty("results")] IReadOnlyList<SmsSendResult>? Results);