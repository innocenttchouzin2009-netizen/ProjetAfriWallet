using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AfriWallet.Ledger.Persistence;

public sealed class LedgerJournalEntityConfiguration : IEntityTypeConfiguration<LedgerJournalEntity>
{
    public void Configure(EntityTypeBuilder<LedgerJournalEntity> builder)
    {
        builder.ToTable("LedgerJournalEntries");
        builder.HasKey(journal => journal.Id);
        builder.Property(journal => journal.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(journal => journal.BusinessReference).HasMaxLength(128).IsRequired();
        builder.Property(journal => journal.CorrelationId).IsRequired();
        builder.Property(journal => journal.PostedAtUtc).IsRequired();
        builder.HasIndex(journal => journal.CorrelationId).IsUnique();
        builder.HasMany(journal => journal.Lines)
            .WithOne(line => line.JournalEntry)
            .HasForeignKey(line => line.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
