using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Notifications.Persistence;

public sealed class NotificationDeliveryDbContext(
    DbContextOptions<NotificationDeliveryDbContext> options) : DbContext(options)
{
    public DbSet<NotificationDeliveryEntity> Deliveries => Set<NotificationDeliveryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var delivery = modelBuilder.Entity<NotificationDeliveryEntity>();
        delivery.ToTable("NotificationDeliveries");
        delivery.HasKey(x => x.DeliveryId);
        delivery.Property(x => x.EventId).IsRequired();
        delivery.Property(x => x.Channel).IsRequired();
        delivery.HasIndex(x => new { x.EventId, x.Channel }).IsUnique();
        delivery.Property(x => x.RecipientUserId).IsRequired();
        delivery.Property(x => x.PaymentRequestId).IsRequired();
        delivery.Property(x => x.EventKind).IsRequired();
        delivery.Property(x => x.CreatedAtUtc).HasMaxLength(64).IsRequired();
        delivery.Property(x => x.TransferId);
        delivery.Property(x => x.Status).IsRequired();
        delivery.Property(x => x.DispatchedAtUtc).HasMaxLength(64);
    }
}

public sealed class NotificationDeliveryEntity
{
    public Guid DeliveryId { get; set; }
    public Guid EventId { get; set; }
    public int Channel { get; set; }
    public Guid RecipientUserId { get; set; }
    public Guid PaymentRequestId { get; set; }
    public int EventKind { get; set; }
    public string CreatedAtUtc { get; set; } = string.Empty;
    public Guid? TransferId { get; set; }
    public int Status { get; set; }
    public string? DispatchedAtUtc { get; set; }
}
