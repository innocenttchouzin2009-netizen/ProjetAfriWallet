using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AfriWallet.Notifications.Application;

namespace AfriWallet.Notifications.Push.Infrastructure;

public sealed class ApnsHttp2PushProviderClient(
    HttpClient httpClient,
    PushProviderOptions options,
    IPushProviderAuthorizationTokenSource tokenSource)
    : IPushProviderClient
{
    public PushProviderKind Provider => PushProviderKind.Apns;

    public bool Supports(PushDevicePlatform platform) => platform == PushDevicePlatform.Ios;

    public async Task<PushDeliveryResult> SendAsync(
        DevicePushRegistration registration,
        PushNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        if (!Supports(registration.Platform))
        {
            return PushDeliveryResult.Permanent("apns-unsupported-platform");
        }

        if (string.IsNullOrWhiteSpace(options.ApnsTopic))
        {
            return PushDeliveryResult.Permanent("apns-topic-not-configured");
        }

        var providerToken = await tokenSource.GetTokenAsync(PushProviderKind.Apns, cancellationToken);
        if (string.IsNullOrWhiteSpace(providerToken))
        {
            return PushDeliveryResult.Permanent("apns-credentials-unavailable");
        }

        var host = options.UseApnsSandbox
            ? "https://api.sandbox.push.apple.com"
            : "https://api.push.apple.com";
        var endpoint = $"{host}/3/device/{Uri.EscapeDataString(registration.Token)}";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", providerToken);
        request.Headers.TryAddWithoutValidation("apns-topic", options.ApnsTopic.Trim());
        request.Headers.TryAddWithoutValidation("apns-push-type", "alert");
        request.Headers.TryAddWithoutValidation("apns-priority", "10");
        request.Headers.TryAddWithoutValidation("apns-id", message.EventId.ToString());
        request.Content = JsonContent.Create(new
        {
            aps = new
            {
                alert = new
                {
                    title = message.Title,
                    body = message.Body
                },
                sound = "default"
            },
            eventId = message.EventId.ToString(),
            deepLink = message.DeepLink
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return PushProviderHttpPolicy.Failure("apns", response);
        }

        string? providerMessageId = null;
        if (response.Headers.TryGetValues("apns-id", out var values))
        {
            providerMessageId = values.FirstOrDefault();
        }

        return PushDeliveryResult.Delivered(providerMessageId);
    }
}
