using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.DeliverySubscriptions.Persistence;

public sealed class PaymentRequestDeliverySubscriptionDbContext(
    DbContextOptions<PaymentRequestDeliverySubscriptionDbContext> options) : DbContext(options)
{
    public DbSet<PaymentRequestDeliverySubscriptionEntity> Subscriptions => Set<PaymentRequestDeliverySubscriptionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<PaymentRequestDeliverySubscriptionEntity>();
        e.ToTable("PaymentRequestDeliverySubscriptions");
        e.HasKey(x => x.Id);
        e.Property(x => x.OwnerId).IsRequired();
        e.Property(x => x.RecipientKind).HasMaxLength(32).IsRequired();
        e.Property(x => x.RecipientKey).HasMaxLength(256).IsRequired();
        e.Property(x => x.EndpointUrl).HasMaxLength(2048).IsRequired();
        e.Property(x => x.Status).IsRequired();
        e.Property(x => x.KeyId).HasMaxLength(64).IsRequired();
        e.Property(x => x.SecretReference).HasMaxLength(128).IsRequired();
        e.Property(x => x.EventTypesJson).IsRequired();
        e.Property(x => x.MaxAttempts).IsRequired();
        e.Property(x => x.BaseRetryDelaySeconds).IsRequired();
        e.Property(x => x.CreatedAtUtc).HasMaxLength(64).IsRequired();
        e.Property(x => x.UpdatedAtUtc).HasMaxLength(64).IsRequired();
        e.HasIndex(x => new { x.OwnerId, x.RecipientKind, x.RecipientKey, x.EndpointUrl }).IsUnique();
        e.HasIndex(x => new { x.RecipientKind, x.RecipientKey, x.Status });
    }
}

public sealed class PaymentRequestDeliverySubscriptionEntity
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string RecipientKind { get; set; } = string.Empty;
    public string RecipientKey { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public int Status { get; set; }
    public string KeyId { get; set; } = string.Empty;
    public string SecretReference { get; set; } = string.Empty;
    public string EventTypesJson { get; set; } = "[]";
    public int MaxAttempts { get; set; }
    public long BaseRetryDelaySeconds { get; set; }
    public string CreatedAtUtc { get; set; } = string.Empty;
    public string UpdatedAtUtc { get; set; } = string.Empty;
}
