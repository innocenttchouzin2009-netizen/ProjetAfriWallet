using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.WebhookSubscriptions.Persistence;

public sealed class PaymentRequestWebhookSubscriptionDbContext(
    DbContextOptions<PaymentRequestWebhookSubscriptionDbContext> options) : DbContext(options)
{
    public DbSet<PaymentRequestWebhookSubscriptionEntity> Subscriptions => Set<PaymentRequestWebhookSubscriptionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PaymentRequestWebhookSubscriptionEntity>();
        entity.ToTable("PaymentRequestWebhookSubscriptions");
        entity.HasKey(x => x.Id);
        entity.Property(x => x.IntegrationId).HasMaxLength(128).IsRequired();
        entity.Property(x => x.EndpointUrl).HasMaxLength(2048).IsRequired();
        entity.Property(x => x.Status).IsRequired();
        entity.Property(x => x.KeyId).HasMaxLength(64).IsRequired();
        entity.Property(x => x.SecretReference).HasMaxLength(128).IsRequired();
        entity.Property(x => x.EventTypesJson).IsRequired();
        entity.Property(x => x.CreatedAtUtc).HasMaxLength(64).IsRequired();
        entity.Property(x => x.UpdatedAtUtc).HasMaxLength(64).IsRequired();
        entity.HasIndex(x => new { x.IntegrationId, x.EndpointUrl }).IsUnique();
        entity.HasIndex(x => x.MerchantId);
        entity.HasIndex(x => x.Status);
    }
}

public sealed class PaymentRequestWebhookSubscriptionEntity
{
    public Guid Id { get; set; }
    public string IntegrationId { get; set; } = string.Empty;
    public Guid? MerchantId { get; set; }
    public string EndpointUrl { get; set; } = string.Empty;
    public int Status { get; set; }
    public string KeyId { get; set; } = string.Empty;
    public string SecretReference { get; set; } = string.Empty;
    public string EventTypesJson { get; set; } = "[]";
    public string CreatedAtUtc { get; set; } = string.Empty;
    public string UpdatedAtUtc { get; set; } = string.Empty;
}
