using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Ledger.Persistence;

public sealed class LedgerDbContext(DbContextOptions<LedgerDbContext> options) : DbContext(options)
{
    public DbSet<LedgerJournalEntity> JournalEntries => Set<LedgerJournalEntity>();
    public DbSet<LedgerLineEntity> Lines => Set<LedgerLineEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new LedgerJournalEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LedgerLineEntityConfiguration());
    }
}
