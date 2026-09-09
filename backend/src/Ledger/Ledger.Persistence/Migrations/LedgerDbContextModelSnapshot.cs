using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace AfriWallet.Ledger.Persistence.Migrations;

[DbContext(typeof(LedgerDbContext))]
partial class LedgerDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.11");

        modelBuilder.Entity("AfriWallet.Ledger.Persistence.LedgerJournalEntity", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("TEXT");
            b.Property<string>("BusinessReference").IsRequired().HasMaxLength(128).HasColumnType("TEXT");
            b.Property<Guid>("CorrelationId").HasColumnType("TEXT");
            b.Property<string>("CurrencyCode").IsRequired().HasMaxLength(3).HasColumnType("TEXT");
            b.Property<DateTimeOffset>("PostedAtUtc").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("CorrelationId").IsUnique();
            b.ToTable("LedgerJournalEntries");
        });

        modelBuilder.Entity("AfriWallet.Ledger.Persistence.LedgerLineEntity", b =>
        {
            b.Property<Guid>("JournalEntryId").HasColumnType("TEXT");
            b.Property<int>("Position").HasColumnType("INTEGER");
            b.Property<Guid>("AccountId").HasColumnType("TEXT");
            b.Property<long>("AmountMinor").HasColumnType("INTEGER");
            b.Property<string>("Memo").HasMaxLength(256).HasColumnType("TEXT");
            b.Property<int>("Side").HasColumnType("INTEGER");
            b.HasKey("JournalEntryId", "Position");
            b.HasIndex("AccountId");
            b.ToTable("LedgerLines");
        });

        modelBuilder.Entity("AfriWallet.Ledger.Persistence.LedgerLineEntity", b =>
        {
            b.HasOne("AfriWallet.Ledger.Persistence.LedgerJournalEntity", "JournalEntry")
                .WithMany("Lines")
                .HasForeignKey("JournalEntryId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            b.Navigation("JournalEntry");
        });

        modelBuilder.Entity("AfriWallet.Ledger.Persistence.LedgerJournalEntity", b =>
        {
            b.Navigation("Lines");
        });
    }
}
