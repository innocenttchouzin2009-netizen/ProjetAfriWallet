namespace AfriWallet.Notifications.Persistence;

public sealed class InAppNotificationEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid EventId { get; set; }
    public Guid PaymentRequestId { get; set; }
    public int EventKind { get; set; }
    public string CreatedAtUtc { get; set; } = string.Empty;
    public string SortKey { get; set; } = string.Empty;
    public Guid? TransferId { get; set; }
    public string? ReadAtUtc { get; set; }
    public string? ArchivedAtUtc { get; set; }
}
