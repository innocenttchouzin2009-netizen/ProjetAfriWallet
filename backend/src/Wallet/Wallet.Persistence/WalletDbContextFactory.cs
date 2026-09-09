using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AfriWallet.Wallet.Persistence;

public sealed class WalletDbContextFactory : IDesignTimeDbContextFactory<WalletDbContext>
{
    public WalletDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__WalletDatabase") ??
            Environment.GetEnvironmentVariable("AFW_WALLET_DB_CONNECTION_STRING") ??
            "Data Source=wallet-registry.db";

        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new WalletDbContext(options);
    }
}
