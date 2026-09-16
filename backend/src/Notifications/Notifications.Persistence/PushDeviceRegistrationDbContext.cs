using AfriWallet.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Notifications.Persistence;

public sealed class PushDeviceRegistrationDbContext(DbContextOptions<PushDeviceRegistrationDbContext> options) : DbContext(options)
{
    public DbSet<PushDeviceRegistrationEntity> PushDeviceRegistrations => Set<PushDeviceRegistrationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var registration = modelBuilder.Entity<PushDeviceRegistrationEntity>();
        registration.ToTable("PushDeviceRegistrations");
        registration.HasKey(x => x.Id);
        registration.Property(x => x.UserId).IsRequired();
        registration.Property(x => x.InstallationId).HasMaxLength(128).IsRequired();
        registration.HasIndex(x => x.InstallationId).IsUnique();
        registration.Property(x => x.Platform).IsRequired();
        registration.Property(x => x.PushToken).HasMaxLength(4096).IsRequired();
        registration.Property(x => x.CreatedAtUtc).HasMaxLength(64).IsRequired();
        registration.Property(x => x.UpdatedAtUtc).HasMaxLength(64).IsRequired();
        registration.Property(x => x.IsActive).IsRequired();
        registration.Property(x => x.DeactivatedAtUtc).HasMaxLength(64);
        registration.HasIndex(x => new { x.UserId, x.IsActive });
    }
}

public sealed class PushDeviceRegistrationEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string InstallationId { get; set; } = string.Empty;
    public PushPlatform Platform { get; set; }
    public string PushToken { get; set; } = string.Empty;
    public string CreatedAtUtc { get; set; } = string.Empty;
    public string UpdatedAtUtc { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? DeactivatedAtUtc { get; set; }
}
