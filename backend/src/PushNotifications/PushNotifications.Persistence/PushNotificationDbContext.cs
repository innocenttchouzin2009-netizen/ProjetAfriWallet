using Microsoft.EntityFrameworkCore;

namespace AfriWallet.PushNotifications.Persistence;

public sealed class PushNotificationDbContext(DbContextOptions<PushNotificationDbContext> options) : DbContext(options)
{
    public DbSet<PushDeviceEntity> PushDevices => Set<PushDeviceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var device = modelBuilder.Entity<PushDeviceEntity>();
        device.ToTable("PushDevices");
        device.HasKey(x => x.Id);
        device.Property(x => x.UserId).IsRequired();
        device.Property(x => x.DeviceId).HasMaxLength(128).IsRequired();
        device.HasIndex(x => new { x.UserId, x.DeviceId }).IsUnique();
        device.Property(x => x.Platform).IsRequired();
        device.Property(x => x.PushToken).HasMaxLength(4096).IsRequired();
        device.Property(x => x.Status).IsRequired();
        device.Property(x => x.RegisteredAtUtc).HasMaxLength(64).IsRequired();
        device.Property(x => x.UpdatedAtUtc).HasMaxLength(64).IsRequired();
        device.Property(x => x.RevokedAtUtc).HasMaxLength(64);
    }
}

public sealed class PushDeviceEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public int Platform { get; set; }
    public string PushToken { get; set; } = string.Empty;
    public int Status { get; set; }
    public string RegisteredAtUtc { get; set; } = string.Empty;
    public string UpdatedAtUtc { get; set; } = string.Empty;
    public string? RevokedAtUtc { get; set; }
}
