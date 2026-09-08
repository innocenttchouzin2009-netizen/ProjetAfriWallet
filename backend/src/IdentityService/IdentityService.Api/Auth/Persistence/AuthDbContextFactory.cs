using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IdentityService.Api.Auth.Persistence;

public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__AuthDatabase") ??
            Environment.GetEnvironmentVariable("AFW_AUTH_DB_CONNECTION_STRING") ??
            "Data Source=identity-auth.db";

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new AuthDbContext(options);
    }
}
