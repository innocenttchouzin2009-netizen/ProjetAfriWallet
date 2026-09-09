using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Wallet.Persistence;

public sealed class WalletDbContext(DbContextOptions<WalletDbContext> options) : DbContext(options)
{
    public DbSet<WalletEntity> Wallets => Set<WalletEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new WalletEntityConfiguration());
    }
}
