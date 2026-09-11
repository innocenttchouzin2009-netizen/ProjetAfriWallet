using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Persistence;

public sealed class PaymentRequestDbContext(DbContextOptions<PaymentRequestDbContext> options) : DbContext(options)
{
    public DbSet<PaymentRequestEntity> PaymentRequests => Set<PaymentRequestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var request = modelBuilder.Entity<PaymentRequestEntity>();
        request.ToTable("PaymentRequests");
        request.HasKey(x => x.Id);
        request.Property(x => x.RequesterWalletId).IsRequired();
        request.Property(x => x.PayerReferenceKind).IsRequired();
        request.Property(x => x.PayerReferenceValue).HasMaxLength(2048).IsRequired();
        request.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        request.Property(x => x.AmountMinor).IsRequired();
        request.Property(x => x.CorrelationId).IsRequired();
        request.HasIndex(x => x.CorrelationId).IsUnique();
        request.Property(x => x.CreatedAtUtc).HasMaxLength(64).IsRequired();
        request.Property(x => x.ExpiresAtUtc).HasMaxLength(64);
        request.Property(x => x.UpdatedAtUtc).HasMaxLength(64).IsRequired();
        request.Property(x => x.Status).IsRequired();
        request.Property(x => x.AcceptedAtUtc).HasMaxLength(64);
        request.Property(x => x.ClosedAtUtc).HasMaxLength(64);
    }
}
