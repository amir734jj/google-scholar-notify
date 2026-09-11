using Newtonsoft.Json;
using Refit;
using ScholarNotify.Interfaces;

namespace ScholarNotify.Services;

public sealed class SmsProxyHubService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    PhoneNumberService phoneNumberService) : IApplicationService
{
    private static readonly RefitSettings RefitSettings = new(new NewtonsoftJsonContentSerializer());

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(ApiToken);

    private string? BaseUrl => configuration["SMS_PROXY_URL"] ?? configuration["SmsProxyHub:BaseUrl"];
    private string? ApiToken => configuration["SMS_PROXY_TOKEN"] ?? configuration["SmsProxyHub:ApiToken"];
    private string? ConnectionId => configuration["SMS_CONNECTION_ID"] ?? configuration["SmsProxyHub:ConnectionId"];

    public async Task SendCitationUpdateAsync(
        string phoneNumber,
        string scholarName,
        int previousCitations,
        int citations,
        long monitorId,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("SMS Proxy Hub is not configured. Set SMS_PROXY_URL and SMS_PROXY_TOKEN.");
        }

        var delta = citations - previousCitations;
        var safeName = scholarName.Length > 70 ? $"{scholarName[..67]}..." : scholarName;
        var message = $"Google Scholar update: {safeName} now has {citations} citations ({delta:+#;-#;0}).";
        var normalizedPhoneNumber = phoneNumberService.NormalizeToE164(phoneNumber);
        var request = new SmsSendRequest(
            Guid.TryParse(ConnectionId, out var connectionId) ? connectionId : null,
            [normalizedPhoneNumber],
            message,
            JsonConvert.SerializeObject(new
            {
                source = "google-scholar-notify",
                monitorId,
                previousCitations,
                citations
            }));

        var httpClient = httpClientFactory.CreateClient("SmsProxyHub");
        httpClient.BaseAddress = new Uri(BaseUrl!.TrimEnd('/'));
        var api = RestService.For<ISmsProxyHubApi>(httpClient, RefitSettings);
        using var response = await api.SendAsync(request, $"Bearer {ApiToken}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = response.Error?.Content ?? string.Empty;
            throw new InvalidOperationException(
                $"SMS Proxy Hub returned HTTP {(int)response.StatusCode}: {body[..Math.Min(body.Length, 180)]}");
        }

        var results = response.Content?.Results;
        var accepted = results is { Count: > 0 }
            && results.All(item => string.Equals(item.Status, "sent", StringComparison.Ordinal));
        if (!accepted)
        {
            throw new InvalidOperationException("SMS Proxy Hub did not accept the notification.");
        }
    }
}