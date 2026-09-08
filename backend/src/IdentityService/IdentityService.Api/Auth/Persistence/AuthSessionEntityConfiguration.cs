using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Api.Auth.Persistence;

public sealed class AuthSessionEntityConfiguration : IEntityTypeConfiguration<AuthSessionEntity>
{
    public void Configure(EntityTypeBuilder<AuthSessionEntity> builder)
    {
        builder.ToTable("auth_sessions");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).ValueGeneratedNever();
        builder.Property(session => session.DeviceId).IsRequired().HasMaxLength(256);
        builder.Property(session => session.RefreshTokenHash).IsRequired().HasMaxLength(128);
        builder.Property(session => session.RevocationReason).HasMaxLength(256);
        builder.Property(session => session.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(session => session.CreatedAtUtc).IsRequired();
        builder.Property(session => session.LastSeenAtUtc).IsRequired();
        builder.Property(session => session.ExpiresAtUtc).IsRequired();
        builder.HasIndex(session => session.RefreshTokenHash).IsUnique();
        builder.HasIndex(session => session.UserId);
        builder.HasIndex(session => session.RefreshTokenFamilyId);
        builder.HasIndex(session => session.Status);
        builder.HasOne<AuthUserEntity>()
            .WithMany()
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
