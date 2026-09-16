using Microsoft.EntityFrameworkCore;

namespace AfriWallet.Notifications.Persistence;

public sealed class NotificationPreferenceDbContext(DbContextOptions<NotificationPreferenceDbContext> options) : DbContext(options)
{
    public DbSet<NotificationPreferenceEntity> NotificationPreferences => Set<NotificationPreferenceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var preference = modelBuilder.Entity<NotificationPreferenceEntity>();
        preference.ToTable("NotificationPreferences");
        preference.HasKey(x => x.Id);
        preference.Property(x => x.UserId).IsRequired();
        preference.Property(x => x.Channel).IsRequired();
        preference.Property(x => x.IsEnabled).IsRequired();
        preference.Property(x => x.CreatedAtUtc).HasMaxLength(64).IsRequired();
        preference.Property(x => x.UpdatedAtUtc).HasMaxLength(64).IsRequired();
        preference.HasIndex(x => new { x.UserId, x.Channel }).IsUnique();
    }
}
