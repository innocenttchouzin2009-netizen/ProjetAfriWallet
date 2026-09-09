using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AfriWallet.Wallet.Persistence;

public sealed class WalletEntityConfiguration : IEntityTypeConfiguration<WalletEntity>
{
    public void Configure(EntityTypeBuilder<WalletEntity> builder)
    {
        builder.ToTable("Wallets");
        builder.HasKey(wallet => wallet.Id);
        builder.Property(wallet => wallet.OwnerId).IsRequired();
        builder.Property(wallet => wallet.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(wallet => wallet.CountryCode).HasMaxLength(2);
        builder.Property(wallet => wallet.Status).IsRequired();
        builder.Property(wallet => wallet.CreatedAtUtc).IsRequired();
        builder.Property(wallet => wallet.UpdatedAtUtc).IsRequired();
        builder.HasIndex(wallet => new { wallet.OwnerId, wallet.CurrencyCode }).IsUnique();
    }
}
