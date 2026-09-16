using AfriWallet.PushNotifications.Domain;

namespace AfriWallet.PushNotifications.Application;

public sealed record RegisterPushDeviceCommand(
    Guid UserId,
    string DeviceId,
    PushPlatform Platform,
    string PushToken,
    DateTimeOffset RegisteredAtUtc);

public sealed record PushDeviceSnapshot(
    Guid Id,
    Guid UserId,
    string DeviceId,
    PushPlatform Platform,
    PushDeviceStatus Status,
    DateTimeOffset RegisteredAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? RevokedAtUtc);

public enum RegisterPushDeviceStatus
{
    Created = 1,
    Refreshed = 2
}

public sealed record RegisterPushDeviceResult(RegisterPushDeviceStatus Status, PushDeviceSnapshot Device);
