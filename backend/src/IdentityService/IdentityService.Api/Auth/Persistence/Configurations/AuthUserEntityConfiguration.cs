using IdentityService.Api.Auth.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Api.Auth.Persistence.Configurations;

public sealed class AuthUserEntityConfiguration : IEntityTypeConfiguration<AuthUserEntity>
{
    public void Configure(EntityTypeBuilder<AuthUserEntity> builder)
    {
        builder.ToTable("auth_users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.NormalizedIdentifier)
            .HasMaxLength(320)
            .IsRequired();
        builder.HasIndex(user => user.NormalizedIdentifier)
            .IsUnique();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(user => user.CreatedAtUtc)
            .IsRequired();
    }
}
