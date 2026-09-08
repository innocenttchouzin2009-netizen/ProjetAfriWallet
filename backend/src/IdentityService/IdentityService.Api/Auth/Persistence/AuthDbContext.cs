using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Auth.Persistence;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<AuthUserEntity> Users => Set<AuthUserEntity>();

    public DbSet<AuthSessionEntity> Sessions => Set<AuthSessionEntity>();

    public DbSet<AuthConsumedRefreshTokenEntity> ConsumedRefreshTokens => Set<AuthConsumedRefreshTokenEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuthUserEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AuthSessionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AuthConsumedRefreshTokenEntityConfiguration());
    }
}
