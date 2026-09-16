using System.Net;
using System.Net.Http.Headers;
using AfriWallet.Notifications.Application;

namespace AfriWallet.Notifications.Push.Infrastructure;

internal static class PushProviderHttpPolicy
{
    public static bool IsRetryable(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout or
            HttpStatusCode.TooManyRequests or
            HttpStatusCode.InternalServerError or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout;

    public static TimeSpan? ReadRetryAfter(HttpResponseHeaders headers)
    {
        var retryAfter = headers.RetryAfter;
        if (retryAfter?.Delta is { } delta && delta >= TimeSpan.Zero)
        {
            return delta;
        }

        if (retryAfter?.Date is { } date)
        {
            var delay = date - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
        }

        return null;
    }

    public static PushDeliveryResult Failure(string provider, HttpResponseMessage response)
    {
        var code = $"{provider}-http-{(int)response.StatusCode}";
        return IsRetryable(response.StatusCode)
            ? PushDeliveryResult.Retryable(code, ReadRetryAfter(response.Headers))
            : PushDeliveryResult.Permanent(code);
    }
}
