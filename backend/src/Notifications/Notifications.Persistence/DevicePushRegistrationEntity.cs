namespace AfriWallet.Notifications.Persistence;

public sealed class DevicePushRegistrationEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public int Platform { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string ProtectedToken { get; set; } = string.Empty;
    public string RegisteredAtUtc { get; set; } = string.Empty;
    public string LastSeenAtUtc { get; set; } = string.Empty;
    public string? RevokedAtUtc { get; set; }
}
