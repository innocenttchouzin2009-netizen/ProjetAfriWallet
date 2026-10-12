using Microsoft.EntityFrameworkCore;

namespace MobileMoney.Production.Payout.Execution.Infrastructure.Persistence;

public sealed class MobileMoneyPayoutExecutionDbContext(
    DbContextOptions<MobileMoneyPayoutExecutionDbContext> options)
    : DbContext(options)
{
    public DbSet<MobileMoneyPayoutExecutionBindingEntity> Bindings =>
        Set<MobileMoneyPayoutExecutionBindingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var binding = modelBuilder.Entity<MobileMoneyPayoutExecutionBindingEntity>();

        binding.ToTable("MobileMoneyPayoutExecutionBindings");
        binding.HasKey(x => x.BindingId);

        binding.Property(x => x.IdempotencyKey)
            .HasMaxLength(256)
            .IsRequired();
        binding.Property(x => x.QuoteSourceCurrency)
            .HasMaxLength(3)
            .IsRequired();
        binding.Property(x => x.QuoteDestinationCurrency)
            .HasMaxLength(3)
            .IsRequired();
        binding.Property(x => x.FundingCurrencyCode)
            .HasMaxLength(3)
            .IsRequired();
        binding.Property(x => x.FundingAllocationsJson)
            .IsRequired();

        binding.HasIndex(x => x.IdempotencyKey)
            .IsUnique();
    }
}
