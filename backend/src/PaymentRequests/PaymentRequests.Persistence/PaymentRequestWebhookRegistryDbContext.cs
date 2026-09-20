using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Persistence;

public sealed class PaymentRequestWebhookRegistryDbContext(
    DbContextOptions<PaymentRequestWebhookRegistryDbContext> options)
    : DbContext(options)
{
    public DbSet<PaymentRequestWebhookDestinationEntity> WebhookDestinations =>
        Set<PaymentRequestWebhookDestinationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var destination = modelBuilder.Entity<PaymentRequestWebhookDestinationEntity>();
        destination.ToTable("PaymentRequestWebhookDestinations");
        destination.HasKey(x => x.Id);
        destination.Property(x => x.RecipientId).IsRequired();
        destination.Property(x => x.Endpoint).HasMaxLength(2048).IsRequired();
        destination.Property(x => x.IsActive).IsRequired();
        destination.Property(x => x.SubscribedEvents).HasMaxLength(256).IsRequired();
        destination.Property(x => x.RetryMaxAttempts).IsRequired();
        destination.Property(x => x.RetryBaseDelayMilliseconds).IsRequired();
        destination.Property(x => x.RetryMaxDelayMilliseconds).IsRequired();
        destination.HasIndex(x => new { x.RecipientId, x.Endpoint }).IsUnique();
        destination.HasIndex(x => new { x.RecipientId, x.IsActive });
    }
}
