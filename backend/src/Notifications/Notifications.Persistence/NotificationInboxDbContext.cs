using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Notifications.Persistence;

public sealed class NotificationInboxDbContext(DbContextOptions<NotificationInboxDbContext> options) : DbContext(options)
{
    public DbSet<InAppNotificationEntity> Notifications => Set<InAppNotificationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<InAppNotificationEntity>();
        entity.ToTable("InAppNotifications");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.UserId).IsRequired();
        entity.Property(x => x.EventId).IsRequired();
        entity.Property(x => x.PaymentRequestId).IsRequired();
        entity.Property(x => x.EventKind).IsRequired();
        entity.Property(x => x.CreatedAtUtc).HasMaxLength(64).IsRequired();
        entity.Property(x => x.ReadAtUtc).HasMaxLength(64);
        entity.HasIndex(x => new { x.UserId, x.EventId }).IsUnique();
        entity.HasIndex(x => new { x.UserId, x.ReadAtUtc });
        entity.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
    }
}
