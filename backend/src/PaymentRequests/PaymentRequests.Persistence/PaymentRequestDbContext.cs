using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PaymentRequests.Persistence;

public sealed class PaymentRequestDbContext(DbContextOptions<PaymentRequestDbContext> options) : DbContext(options)
{
    public DbSet<PaymentRequestEntity> PaymentRequests => Set<PaymentRequestEntity>();
    public DbSet<PaymentRequestEventOutboxEntity> PaymentRequestEventOutbox => Set<PaymentRequestEventOutboxEntity>();
    public DbSet<PaymentRequestEventAttemptEntity> PaymentRequestEventAttempts => Set<PaymentRequestEventAttemptEntity>();

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
        request.HasIndex(x => new { x.RequesterWalletId, x.CreatedAtUtc, x.Id });
        request.HasIndex(x => new { x.PayerReferenceKind, x.PayerReferenceValue, x.CreatedAtUtc, x.Id });
        request.HasIndex(x => new { x.AcceptedPayerWalletId, x.CreatedAtUtc, x.Id });
        request.HasIndex(x => new { x.Status, x.CreatedAtUtc, x.Id });

        var outbox = modelBuilder.Entity<PaymentRequestEventOutboxEntity>();
        outbox.ToTable("PaymentRequestEventOutbox");
        outbox.HasKey(x => x.EventId);
        outbox.Property(x => x.PaymentRequestId).IsRequired();
        outbox.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        outbox.Property(x => x.PayloadJson).IsRequired();
        outbox.Property(x => x.OccurredAtUtc).IsRequired();
        outbox.Property(x => x.EnqueuedAtUtc).IsRequired();
        outbox.Property(x => x.AvailableAtUtc).IsRequired();
        outbox.Property(x => x.Status).IsRequired();
        outbox.Property(x => x.AttemptCount).IsRequired();
        outbox.Property(x => x.LastError).HasMaxLength(2048);
        outbox.HasIndex(x => new { x.Status, x.AvailableAtUtc, x.EnqueuedAtUtc });
        outbox.HasIndex(x => x.PaymentRequestId);
        outbox.HasIndex(x => x.LeaseToken);

        var attempt = modelBuilder.Entity<PaymentRequestEventAttemptEntity>();
        attempt.ToTable("PaymentRequestEventAttempts");
        attempt.HasKey(x => x.AttemptId);
        attempt.Property(x => x.EventId).IsRequired();
        attempt.Property(x => x.AttemptNumber).IsRequired();
        attempt.Property(x => x.StartedAtUtc).IsRequired();
        attempt.Property(x => x.Error).HasMaxLength(2048);
        attempt.HasIndex(x => new { x.EventId, x.AttemptNumber }).IsUnique();
        attempt.HasIndex(x => x.EventId);
    }
}
