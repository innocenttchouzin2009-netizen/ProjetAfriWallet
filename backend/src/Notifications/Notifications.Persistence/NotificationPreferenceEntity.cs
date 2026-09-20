namespace AfriWallet.Notifications.Persistence;

public sealed class NotificationPreferenceEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int Channel { get; set; }
    public bool IsEnabled { get; set; }
    public string CreatedAtUtc { get; set; } = string.Empty;
    public string UpdatedAtUtc { get; set; } = string.Empty;
}
