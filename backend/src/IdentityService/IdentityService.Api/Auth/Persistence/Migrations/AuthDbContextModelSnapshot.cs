using IdentityService.Api.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace IdentityService.Api.Auth.Persistence.Migrations;

[DbContext(typeof(AuthDbContext))]
public partial class AuthDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.11");

        modelBuilder.Entity<AuthUserEntity>(builder =>
        {
            builder.Property(user => user.Id)
                .ValueGeneratedNever()
                .HasColumnType("TEXT");
            builder.Property(user => user.NormalizedIdentifier)
                .IsRequired()
                .HasMaxLength(320)
                .HasColumnType("TEXT");
            builder.Property(user => user.PasswordHash)
                .IsRequired()
                .HasMaxLength(1024)
                .HasColumnType("TEXT");
            builder.Property(user => user.IsDisabled)
                .HasColumnType("INTEGER");
            builder.Property(user => user.CreatedAtUtc)
                .HasColumnType("TEXT");
            builder.HasKey(user => user.Id);
            builder.HasIndex(user => user.NormalizedIdentifier).IsUnique();
            builder.ToTable("auth_users");
        });

        modelBuilder.Entity<AuthSessionEntity>(builder =>
        {
            builder.Property(session => session.Id)
                .ValueGeneratedNever()
                .HasColumnType("TEXT");
            builder.Property(session => session.UserId).HasColumnType("TEXT");
            builder.Property(session => session.DeviceId)
                .IsRequired()
                .HasMaxLength(256)
                .HasColumnType("TEXT");
            builder.Property(session => session.RefreshTokenHash)
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnType("TEXT");
            builder.Property(session => session.RefreshTokenFamilyId).HasColumnType("TEXT");
            builder.Property(session => session.CreatedAtUtc).HasColumnType("TEXT");
            builder.Property(session => session.LastSeenAtUtc).HasColumnType("TEXT");
            builder.Property(session => session.ExpiresAtUtc).HasColumnType("TEXT");
            builder.Property(session => session.RevokedAtUtc).HasColumnType("TEXT");
            builder.Property(session => session.RevocationReason)
                .HasMaxLength(256)
                .HasColumnType("TEXT");
            builder.Property(session => session.TokenVersion).HasColumnType("INTEGER");
            builder.Property(session => session.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(32)
                .HasColumnType("TEXT");
            builder.HasKey(session => session.Id);
            builder.HasIndex(session => session.RefreshTokenFamilyId);
            builder.HasIndex(session => session.RefreshTokenHash).IsUnique();
            builder.HasIndex(session => session.Status);
            builder.HasIndex(session => session.UserId);
            builder.ToTable("auth_sessions");
        });

        modelBuilder.Entity<AuthConsumedRefreshTokenEntity>(builder =>
        {
            builder.Property(token => token.RefreshTokenHash)
                .HasMaxLength(128)
                .HasColumnType("TEXT");
            builder.Property(token => token.SessionId).HasColumnType("TEXT");
            builder.Property(token => token.ConsumedAtUtc).HasColumnType("TEXT");
            builder.HasKey(token => token.RefreshTokenHash);
            builder.HasIndex(token => token.SessionId);
            builder.ToTable("auth_consumed_refresh_tokens");
        });

        modelBuilder.Entity<AuthSessionEntity>(builder =>
        {
            builder.HasOne<AuthUserEntity>()
                .WithMany()
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity<AuthConsumedRefreshTokenEntity>(builder =>
        {
            builder.HasOne<AuthSessionEntity>()
                .WithMany()
                .HasForeignKey(token => token.SessionId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });
    }
}
