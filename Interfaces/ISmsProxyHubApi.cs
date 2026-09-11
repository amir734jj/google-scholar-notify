using Refit;

namespace ScholarNotify.Interfaces;

public interface ISmsProxyHubApi
{
    [Post("/api/messages/send")]
    Task<ApiResponse<SmsSendResponse>> SendAsync(
        [Body] SmsSendRequest request,
        [Header("Authorization")] string authorization,
        CancellationToken cancellationToken = default);
}