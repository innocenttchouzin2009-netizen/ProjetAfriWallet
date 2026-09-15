using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Notifications.Persistence;

public sealed class NotificationInboxDbContext(DbContextOptions<NotificationInboxDbContext> options) : DbContext(options)
{
    public DbSet<InAppNotificationEntity> Notifications => Set<InAppNotificationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var notification = modelBuilder.Entity<InAppNotificationEntity>();
        notification.ToTable("InAppNotifications");
        notification.HasKey(x => x.Id);
        notification.Property(x => x.UserId).IsRequired();
        notification.Property(x => x.EventId).IsRequired();
        notification.Property(x => x.PaymentRequestId).IsRequired();
        notification.Property(x => x.EventKind).IsRequired();
        notification.Property(x => x.CreatedAtUtc).HasMaxLength(64).IsRequired();
        notification.Property(x => x.SortKey).HasMaxLength(64).IsRequired();
        notification.Property(x => x.TransferId);
        notification.Property(x => x.ReadAtUtc).HasMaxLength(64);
        notification.Property(x => x.ArchivedAtUtc).HasMaxLength(64);
        notification.HasIndex(x => new { x.UserId, x.EventId }).IsUnique();
        notification.HasIndex(x => new { x.UserId, x.SortKey });
        notification.HasIndex(x => new { x.UserId, x.ArchivedAtUtc, x.ReadAtUtc });
    }
}
