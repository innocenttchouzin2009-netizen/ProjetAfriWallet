using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AfriWallet.Ledger.Persistence;

public sealed class LedgerDbContextFactory : IDesignTimeDbContextFactory<LedgerDbContext>
{
    public LedgerDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__LedgerDatabase") ??
            Environment.GetEnvironmentVariable("AFW_LEDGER_DB_CONNECTION_STRING") ??
            "Data Source=ledger.db";

        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new LedgerDbContext(options);
    }
}
