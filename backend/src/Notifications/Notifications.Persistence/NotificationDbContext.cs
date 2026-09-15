using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Notifications.Persistence;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<InAppNotificationEntity> InAppNotifications => Set<InAppNotificationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var notification = modelBuilder.Entity<InAppNotificationEntity>();
        notification.ToTable("InAppNotifications");
        notification.HasKey(x => x.NotificationId);
        notification.Property(x => x.SourceEventId).IsRequired();
        notification.HasIndex(x => x.SourceEventId).IsUnique();
        notification.Property(x => x.RecipientUserId).IsRequired();
        notification.HasIndex(x => new { x.RecipientUserId, x.CreatedAtUtc });
        notification.Property(x => x.PaymentRequestId).IsRequired();
        notification.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        notification.Property(x => x.AmountMinor).IsRequired();
        notification.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        notification.Property(x => x.Title).HasMaxLength(256).IsRequired();
        notification.Property(x => x.Body).HasMaxLength(2048).IsRequired();
        notification.Property(x => x.CreatedAtUtc).HasMaxLength(64).IsRequired();
        notification.Property(x => x.IsRead).IsRequired();
        notification.Property(x => x.ReadAtUtc).HasMaxLength(64);
    }
}

public sealed class InAppNotificationEntity
{
    public Guid NotificationId { get; set; }
    public Guid SourceEventId { get; set; }
    public Guid RecipientUserId { get; set; }
    public Guid PaymentRequestId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public long AmountMinor { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string CreatedAtUtc { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string? ReadAtUtc { get; set; }
}
