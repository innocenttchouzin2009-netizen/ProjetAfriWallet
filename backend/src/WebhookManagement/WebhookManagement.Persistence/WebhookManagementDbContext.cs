using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Webhooks.Persistence;

public sealed class WebhookManagementDbContext(DbContextOptions<WebhookManagementDbContext> options) : DbContext(options)
{
    public DbSet<WebhookSubscriptionEntity> WebhookSubscriptions => Set<WebhookSubscriptionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var subscription = modelBuilder.Entity<WebhookSubscriptionEntity>();
        subscription.ToTable("WebhookSubscriptions");
        subscription.HasKey(x => x.Id);
        subscription.Property(x => x.OwnerId).IsRequired();
        subscription.Property(x => x.Endpoint).HasMaxLength(2048).IsRequired();
        subscription.Property(x => x.EventTypesJson).IsRequired();
        subscription.Property(x => x.SigningKeyReference).HasMaxLength(256).IsRequired();
        subscription.Property(x => x.Status).IsRequired();
        subscription.Property(x => x.CreatedAtUtc).HasMaxLength(64).IsRequired();
        subscription.Property(x => x.UpdatedAtUtc).HasMaxLength(64).IsRequired();
        subscription.HasIndex(x => new { x.OwnerId, x.Status });
        subscription.HasIndex(x => new { x.OwnerId, x.CreatedAtUtc, x.Id });
    }
}
