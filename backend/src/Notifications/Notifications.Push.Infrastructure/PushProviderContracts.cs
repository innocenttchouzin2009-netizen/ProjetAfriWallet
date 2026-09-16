using AfriWallet.Notifications.Application;

namespace AfriWallet.Notifications.Push.Infrastructure;

public enum PushProviderKind
{
    Fcm = 1,
    Apns = 2
}

public sealed record PushProviderOptions(
    string? FcmProjectId,
    string? ApnsTopic,
    bool UseApnsSandbox);

public interface IPushProviderAuthorizationTokenSource
{
    Task<string?> GetTokenAsync(
        PushProviderKind provider,
        CancellationToken cancellationToken = default);
}

public interface IPushProviderClient
{
    PushProviderKind Provider { get; }
    bool Supports(PushDevicePlatform platform);

    Task<PushDeliveryResult> SendAsync(
        DevicePushRegistration registration,
        PushNotificationMessage message,
        CancellationToken cancellationToken = default);
}
