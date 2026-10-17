using Microsoft.EntityFrameworkCore;

namespace MobileMoney.Production.Payout.Funding.Infrastructure.Persistence;

public sealed class MobileMoneyPayoutFundingDbContext(
    DbContextOptions<MobileMoneyPayoutFundingDbContext> options)
    : DbContext(options)
{
    public DbSet<MobileMoneyPayoutFundingPlanEntity> Plans =>
        Set<MobileMoneyPayoutFundingPlanEntity>();

    public DbSet<MobileMoneyPayoutFundingAttemptEntity> Attempts =>
        Set<MobileMoneyPayoutFundingAttemptEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var plan = modelBuilder.Entity<MobileMoneyPayoutFundingPlanEntity>();
        plan.ToTable("MobileMoneyPayoutFundingPlans");
        plan.HasKey(x => x.CorrelationId);
        plan.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();

        var attempt = modelBuilder.Entity<MobileMoneyPayoutFundingAttemptEntity>();
        attempt.ToTable("MobileMoneyPayoutFundingAttempts");
        attempt.HasKey(x => x.Id);
        attempt.Property(x => x.SourceId).HasMaxLength(128).IsRequired();
        attempt.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        attempt.Property(x => x.StatusReason).HasMaxLength(512);

        attempt.HasIndex(x => new { x.CorrelationId, x.Sequence }).IsUnique();
        attempt.HasIndex(x => new { x.CorrelationId, x.SourceId }).IsUnique();

        attempt.HasOne<MobileMoneyPayoutFundingPlanEntity>()
            .WithMany()
            .HasForeignKey(x => x.CorrelationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
