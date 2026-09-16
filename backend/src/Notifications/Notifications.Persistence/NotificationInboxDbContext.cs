using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Notifications.Persistence;

public sealed class NotificationInboxDbContext(DbContextOptions<NotificationInboxDbContext> options) : DbContext(options)
{
    public DbSet<InAppNotificationEntity> Notifications => Set<InAppNotificationEntity>();
    public DbSet<DevicePushRegistrationEntity> DevicePushRegistrations => Set<DevicePushRegistrationEntity>();

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

        var push = modelBuilder.Entity<DevicePushRegistrationEntity>();
        push.ToTable("DevicePushRegistrations");
        push.HasKey(x => x.Id);
        push.Property(x => x.UserId).IsRequired();
        push.Property(x => x.DeviceId).HasMaxLength(128).IsRequired();
        push.Property(x => x.Platform).IsRequired();
        push.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        push.Property(x => x.ProtectedToken).HasMaxLength(8192).IsRequired();
        push.Property(x => x.RegisteredAtUtc).HasMaxLength(64).IsRequired();
        push.Property(x => x.LastSeenAtUtc).HasMaxLength(64).IsRequired();
        push.Property(x => x.RevokedAtUtc).HasMaxLength(64);
        push.HasIndex(x => new { x.UserId, x.DeviceId }).IsUnique().HasFilter("\"RevokedAtUtc\" IS NULL");
        push.HasIndex(x => x.TokenHash).IsUnique().HasFilter("\"RevokedAtUtc\" IS NULL");
        push.HasIndex(x => new { x.UserId, x.RevokedAtUtc });
    }
}
