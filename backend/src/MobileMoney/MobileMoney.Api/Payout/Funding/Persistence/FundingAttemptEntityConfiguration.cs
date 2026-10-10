using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MobileMoney.Production.Payout.Funding.Persistence;

public sealed class FundingAttemptEntityConfiguration
    : IEntityTypeConfiguration<FundingAttemptEntity>
{
    public void Configure(EntityTypeBuilder<FundingAttemptEntity> builder)
    {
        builder.ToTable("MobileMoneyPayoutFundingAttempts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CorrelationId).IsRequired();
        builder.Property(x => x.SourceId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SourceType).IsRequired();
        builder.Property(x => x.AmountMinor).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasMaxLength(64).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasMaxLength(64).IsRequired();
        builder.Property(x => x.StatusReason).HasMaxLength(2048);

        builder.HasIndex(x => new { x.CorrelationId, x.CreatedAtUtc, x.Id });
    }
}
