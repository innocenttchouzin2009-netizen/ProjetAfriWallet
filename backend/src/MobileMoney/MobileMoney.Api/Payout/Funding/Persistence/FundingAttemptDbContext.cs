using Microsoft.EntityFrameworkCore;

namespace MobileMoney.Production.Payout.Funding.Persistence;

public sealed class FundingAttemptDbContext(
    DbContextOptions<FundingAttemptDbContext> options)
    : DbContext(options)
{
    public DbSet<FundingAttemptEntity> FundingAttempts =>
        Set<FundingAttemptEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(
            new FundingAttemptEntityConfiguration());
    }
}
