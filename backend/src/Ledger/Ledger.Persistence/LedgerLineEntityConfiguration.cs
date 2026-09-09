using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AfriWallet.Ledger.Persistence;

public sealed class LedgerLineEntityConfiguration : IEntityTypeConfiguration<LedgerLineEntity>
{
    public void Configure(EntityTypeBuilder<LedgerLineEntity> builder)
    {
        builder.ToTable("LedgerLines");
        builder.HasKey(line => new { line.JournalEntryId, line.Position });
        builder.Property(line => line.AccountId).IsRequired();
        builder.Property(line => line.Side).IsRequired();
        builder.Property(line => line.AmountMinor).IsRequired();
        builder.Property(line => line.Memo).HasMaxLength(256);
        builder.HasIndex(line => line.AccountId);
    }
}
