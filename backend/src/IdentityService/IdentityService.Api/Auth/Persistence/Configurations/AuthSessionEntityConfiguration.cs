using IdentityService.Api.Auth.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Api.Auth.Persistence.Configurations;

public sealed class AuthSessionEntityConfiguration : IEntityTypeConfiguration<AuthSessionEntity>
{
    public void Configure(EntityTypeBuilder<AuthSessionEntity> builder)
    {
        builder.ToTable("auth_sessions");
        builder.HasKey(session => session.Id);

        builder.Property(session => session.DeviceId)
            .HasMaxLength(256)
            .IsRequired();
        builder.Property(session => session.RefreshTokenHash)
            .HasMaxLength(128)
            .IsRequired();
        builder.Property(session => session.RevocationReason)
            .HasMaxLength(256);
        builder.Property(session => session.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(session => session.UserId);
        builder.HasIndex(session => session.RefreshTokenHash)
            .IsUnique();
        builder.HasIndex(session => session.RefreshTokenFamilyId);
        builder.HasIndex(session => new { session.UserId, session.Status });
    }
}
