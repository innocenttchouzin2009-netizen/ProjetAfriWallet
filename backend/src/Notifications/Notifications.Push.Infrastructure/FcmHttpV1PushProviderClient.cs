using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AfriWallet.Notifications.Application;

namespace AfriWallet.Notifications.Push.Infrastructure;

public sealed class FcmHttpV1PushProviderClient(
    HttpClient httpClient,
    PushProviderOptions options,
    IPushProviderAuthorizationTokenSource tokenSource)
    : IPushProviderClient
{
    public PushProviderKind Provider => PushProviderKind.Fcm;

    public bool Supports(PushDevicePlatform platform) =>
        platform is PushDevicePlatform.Android or PushDevicePlatform.Web;

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
            return PushDeliveryResult.Permanent("fcm-unsupported-platform");
        }

        if (string.IsNullOrWhiteSpace(options.FcmProjectId))
        {
            return PushDeliveryResult.Permanent("fcm-project-not-configured");
        }

        var accessToken = await tokenSource.GetTokenAsync(PushProviderKind.Fcm, cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return PushDeliveryResult.Permanent("fcm-credentials-unavailable");
        }

        var endpoint = $"https://fcm.googleapis.com/v1/projects/{Uri.EscapeDataString(options.FcmProjectId.Trim())}/messages:send";
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new
        {
            message = new
            {
                token = registration.Token,
                notification = new
                {
                    title = message.Title,
                    body = message.Body
                },
                data = new Dictionary<string, string>
                {
                    ["eventId"] = message.EventId.ToString(),
                    ["deepLink"] = message.DeepLink ?? string.Empty
                }
            }
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return PushProviderHttpPolicy.Failure("fcm", response);
        }

        string? providerMessageId = null;
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(payload))
        {
            try
            {
                using var document = JsonDocument.Parse(payload);
                if (document.RootElement.TryGetProperty("name", out var name))
                {
                    providerMessageId = name.GetString();
                }
            }
            catch (JsonException)
            {
                providerMessageId = null;
            }
        }

        return PushDeliveryResult.Delivered(providerMessageId);
    }
}
