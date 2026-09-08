using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Api.Auth.Persistence;

public sealed class AuthUserEntityConfiguration : IEntityTypeConfiguration<AuthUserEntity>
{
    public void Configure(EntityTypeBuilder<AuthUserEntity> builder)
    {
        builder.ToTable("auth_users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Property(user => user.NormalizedIdentifier).IsRequired().HasMaxLength(320);
        builder.Property(user => user.PasswordHash).IsRequired().HasMaxLength(1024);
        builder.Property(user => user.CreatedAtUtc).IsRequired();
        builder.HasIndex(user => user.NormalizedIdentifier).IsUnique();
    }
}
