using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PartnerWebhooks.Persistence;

public sealed class PartnerWebhookDbContext(DbContextOptions<PartnerWebhookDbContext> options) : DbContext(options)
{
    public DbSet<PartnerWebhookSubscriptionEntity> Subscriptions => Set<PartnerWebhookSubscriptionEntity>();
    public DbSet<PartnerWebhookSubscriptionEventTypeEntity> EventTypes => Set<PartnerWebhookSubscriptionEventTypeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var subscription = modelBuilder.Entity<PartnerWebhookSubscriptionEntity>();
        subscription.ToTable("PartnerWebhookSubscriptions");
        subscription.HasKey(x => x.Id);
        subscription.Property(x => x.PartnerId).HasMaxLength(128).IsRequired();
        subscription.HasIndex(x => x.PartnerId);
        subscription.Property(x => x.Endpoint).HasMaxLength(2048).IsRequired();
        subscription.Property(x => x.SigningSecretReference).HasMaxLength(256).IsRequired();
        subscription.Property(x => x.Status).IsRequired();
        subscription.HasIndex(x => x.Status);
        subscription.Property(x => x.CreatedAtUtc).HasMaxLength(64).IsRequired();
        subscription.Property(x => x.UpdatedAtUtc).HasMaxLength(64).IsRequired();
        subscription.HasMany(x => x.EventTypes)
            .WithOne()
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        var eventType = modelBuilder.Entity<PartnerWebhookSubscriptionEventTypeEntity>();
        eventType.ToTable("PartnerWebhookSubscriptionEventTypes");
        eventType.HasKey(x => new { x.SubscriptionId, x.EventType });
        eventType.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        eventType.HasIndex(x => x.EventType);
    }
}

public sealed class PartnerWebhookSubscriptionEntity
{
    public Guid Id { get; set; }
    public string PartnerId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string SigningSecretReference { get; set; } = string.Empty;
    public int Status { get; set; }
    public string CreatedAtUtc { get; set; } = string.Empty;
    public string UpdatedAtUtc { get; set; } = string.Empty;
    public List<PartnerWebhookSubscriptionEventTypeEntity> EventTypes { get; set; } = [];
}

public sealed class PartnerWebhookSubscriptionEventTypeEntity
{
    public Guid SubscriptionId { get; set; }
    public string EventType { get; set; } = string.Empty;
}
