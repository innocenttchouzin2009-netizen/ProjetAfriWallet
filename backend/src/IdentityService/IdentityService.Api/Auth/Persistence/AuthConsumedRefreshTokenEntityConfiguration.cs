using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Api.Auth.Persistence;

public sealed class AuthConsumedRefreshTokenEntityConfiguration : IEntityTypeConfiguration<AuthConsumedRefreshTokenEntity>
{
    public void Configure(EntityTypeBuilder<AuthConsumedRefreshTokenEntity> builder)
    {
        builder.ToTable("auth_consumed_refresh_tokens");
        builder.HasKey(token => token.RefreshTokenHash);
        builder.Property(token => token.RefreshTokenHash).HasMaxLength(128);
        builder.Property(token => token.ConsumedAtUtc).IsRequired();
        builder.HasIndex(token => token.SessionId);
        builder.HasOne<AuthSessionEntity>()
            .WithMany()
            .HasForeignKey(token => token.SessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
