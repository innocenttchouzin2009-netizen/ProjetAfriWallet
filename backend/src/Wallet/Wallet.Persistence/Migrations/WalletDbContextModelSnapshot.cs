using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AfriWallet.Wallet.Persistence.Migrations;

[DbContext(typeof(WalletDbContext))]
partial class WalletDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "10.0.11");

        modelBuilder.Entity("AfriWallet.Wallet.Persistence.WalletEntity", b =>
        {
            b.Property<Guid>("Id").HasColumnType("TEXT");
            b.Property<string>("CountryCode").HasMaxLength(2).HasColumnType("TEXT");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("TEXT");
            b.Property<string>("CurrencyCode").IsRequired().HasMaxLength(3).HasColumnType("TEXT");
            b.Property<Guid>("OwnerId").HasColumnType("TEXT");
            b.Property<int>("Status").HasColumnType("INTEGER");
            b.Property<DateTimeOffset>("UpdatedAtUtc").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("OwnerId", "CurrencyCode").IsUnique();
            b.ToTable("Wallets");
        });
#pragma warning restore 612, 618
    }
}
