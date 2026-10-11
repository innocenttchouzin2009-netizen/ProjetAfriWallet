using Microsoft.EntityFrameworkCore;

namespace MobileMoney.Production.Payout.Quote.Infrastructure.Persistence;

public sealed class MobileMoneyPayoutQuoteDbContext(
    DbContextOptions<MobileMoneyPayoutQuoteDbContext> options)
    : DbContext(options)
{
    public DbSet<MobileMoneyPayoutQuoteEntity> Quotes =>
        Set<MobileMoneyPayoutQuoteEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var quote = modelBuilder.Entity<MobileMoneyPayoutQuoteEntity>();

        quote.ToTable("MobileMoneyPayoutQuotes");
        quote.HasKey(x => x.QuoteId);

        quote.Property(x => x.SourceCountryCode)
            .HasMaxLength(2)
            .IsRequired();
        quote.Property(x => x.SourceCurrency)
            .HasMaxLength(3)
            .IsRequired();
        quote.Property(x => x.DestinationCountryCode)
            .HasMaxLength(2)
            .IsRequired();
        quote.Property(x => x.DestinationCurrency)
            .HasMaxLength(3)
            .IsRequired();
        quote.Property(x => x.OperatorCode)
            .HasMaxLength(64)
            .IsRequired();
        quote.Property(x => x.FeesJson)
            .IsRequired();

        quote.HasIndex(x => x.FundingCorrelationId)
            .IsUnique();
    }
}
