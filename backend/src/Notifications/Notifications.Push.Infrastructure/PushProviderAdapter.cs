using AfriWallet.Notifications.Application;
using AfriWallet.Notifications.Domain;

namespace AfriWallet.Notifications.Push.Infrastructure;

public sealed record PushProviderRequest(
    string PushToken,
    Guid NotificationId,
    string Title,
    string Body,
    IReadOnlyDictionary<string, string> Data);

public enum PushProviderFailureKind
{
    InvalidToken = 1,
    Transient = 2,
    Permanent = 3
}

public sealed record PushProviderResult(
    bool Accepted,
    string? ProviderMessageId,
    PushProviderFailureKind? FailureKind)
{
    public static PushProviderResult Delivered(string? providerMessageId = null) => new(true, providerMessageId, null);
    public static PushProviderResult Failed(PushProviderFailureKind failureKind) => new(false, null, failureKind);
}

public interface IAndroidPushProviderClient
{
    Task<PushProviderResult> SendAsync(PushProviderRequest request, CancellationToken cancellationToken = default);
}

public interface IIosPushProviderClient
{
    Task<PushProviderResult> SendAsync(PushProviderRequest request, CancellationToken cancellationToken = default);
}

public sealed class PlatformPushDeliveryAdapter(
    IAndroidPushProviderClient androidClient,
    IIosPushProviderClient iosClient) : IPushDeliveryPort
{
    public async Task<PushDeliveryResult> DeliverAsync(
        PushDeliveryTarget target,
        PushNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        var providerRequest = new PushProviderRequest(
            target.PushToken,
            message.NotificationId,
            message.Title,
            message.Body,
            message.Data);

        var providerResult = target.Platform switch
        {
            PushPlatform.Android => await androidClient.SendAsync(providerRequest, cancellationToken),
            PushPlatform.Ios => await iosClient.SendAsync(providerRequest, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(target.Platform))
        };

        if (providerResult.Accepted)
            return PushDeliveryResult.Delivered(providerResult.ProviderMessageId);

        var failureKind = providerResult.FailureKind ?? PushProviderFailureKind.Permanent;
        return PushDeliveryResult.Failed(failureKind switch
        {
            PushProviderFailureKind.InvalidToken => PushDeliveryFailureKind.InvalidToken,
            PushProviderFailureKind.Transient => PushDeliveryFailureKind.Transient,
            _ => PushDeliveryFailureKind.Permanent
        });
    }
}
