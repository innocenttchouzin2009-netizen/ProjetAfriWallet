using Microsoft.EntityFrameworkCore;

namespace Treasury.Infrastructure.Persistence;

public sealed class TreasuryDbContext(DbContextOptions<TreasuryDbContext> options) : DbContext(options)
{
    public DbSet<TreasuryAccountEntity> Accounts => Set<TreasuryAccountEntity>();
    public DbSet<TreasuryTransactionEntity> Transactions => Set<TreasuryTransactionEntity>();
    public DbSet<TreasuryEntryEntity> Entries => Set<TreasuryEntryEntity>();
    public DbSet<TreasuryReservationEntity> Reservations => Set<TreasuryReservationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TreasuryAccountEntity>(entity =>
        {
            entity.ToTable("TreasuryAccounts");
            entity.HasKey(x => x.AccountId);
            entity.Property(x => x.AccountCode).HasMaxLength(128).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.HasIndex(x => x.AccountCode).IsUnique();
        });

        modelBuilder.Entity<TreasuryTransactionEntity>(entity =>
        {
            entity.ToTable("TreasuryTransactions");
            entity.HasKey(x => x.TransactionId);
            entity.Property(x => x.Reference).HasMaxLength(256).IsRequired();
            entity.Property(x => x.CorrelationId).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.CorrelationId).IsUnique();
        });

        modelBuilder.Entity<TreasuryEntryEntity>(entity =>
        {
            entity.ToTable("TreasuryEntries");
            entity.HasKey(x => x.EntryId);
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Reference).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.AccountId);
            entity.HasOne<TreasuryTransactionEntity>()
                .WithMany()
                .HasForeignKey(x => x.TransactionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TreasuryReservationEntity>(entity =>
        {
            entity.ToTable("TreasuryReservations");
            entity.HasKey(x => x.ReservationId);
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Reference).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.AccountId);
        });
    }
}

public sealed class TreasuryAccountEntity
{
    public Guid AccountId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public int Type { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class TreasuryTransactionEntity
{
    public Guid TransactionId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PostedAtUtc { get; set; }
}

public sealed class TreasuryEntryEntity
{
    public Guid EntryId { get; set; }
    public Guid TransactionId { get; set; }
    public Guid AccountId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public long DebitMinor { get; set; }
    public long CreditMinor { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime PostedAtUtc { get; set; }
}

public sealed class TreasuryReservationEntity
{
    public Guid ReservationId { get; set; }
    public Guid AccountId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public long AmountMinor { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReleasedAtUtc { get; set; }
}
