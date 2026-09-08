using IdentityService.Api.Auth.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Api.Auth.Persistence.Configurations;

public sealed class ConsumedRefreshTokenEntityConfiguration : IEntityTypeConfiguration<ConsumedRefreshTokenEntity>
{
    public void Configure(EntityTypeBuilder<ConsumedRefreshTokenEntity> builder)
    {
        builder.ToTable("auth_consumed_refresh_tokens");
        builder.HasKey(token => token.RefreshTokenHash);

        builder.Property(token => token.RefreshTokenHash)
            .HasMaxLength(128);

        builder.HasIndex(token => token.SessionId);
        builder.HasIndex(token => token.RefreshTokenFamilyId);
        builder.HasIndex(token => token.ConsumedAtUtc);
    }
}
