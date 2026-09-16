using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Notification.Persistence;

public sealed class PaymentRequestNotificationDbContext(DbContextOptions<PaymentRequestNotificationDbContext> options) : DbContext(options)
{
    public DbSet<PaymentRequestNotificationEntity> Notifications => Set<PaymentRequestNotificationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var notification = modelBuilder.Entity<PaymentRequestNotificationEntity>();
        notification.ToTable("PaymentRequestNotifications");
        notification.HasKey(x => x.Id);
        notification.Property(x => x.SourceEventId).IsRequired();
        notification.Property(x => x.PaymentRequestId).IsRequired();
        notification.Property(x => x.Kind).IsRequired();
        notification.Property(x => x.Audience).IsRequired();
        notification.Property(x => x.RecipientOwnerId).IsRequired();
        notification.Property(x => x.RequesterWalletId).IsRequired();
        notification.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        notification.Property(x => x.AmountMinor).IsRequired();
        notification.Property(x => x.OccurredAtUtc).IsRequired();
        notification.Property(x => x.RequestStatus).IsRequired();
        notification.Property(x => x.ReadStatus).IsRequired();
        notification.HasIndex(x => new { x.SourceEventId, x.RecipientOwnerId, x.Audience }).IsUnique();
    }
}
