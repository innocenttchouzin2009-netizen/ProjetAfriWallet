using IdentityService.Api.Auth.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Auth.Persistence;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<AuthUserEntity> Users => Set<AuthUserEntity>();
    public DbSet<AuthSessionEntity> Sessions => Set<AuthSessionEntity>();
    public DbSet<ConsumedRefreshTokenEntity> ConsumedRefreshTokens => Set<ConsumedRefreshTokenEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuthDbContext).Assembly);
    }
}
