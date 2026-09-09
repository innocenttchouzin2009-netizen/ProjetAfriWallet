using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AfriWallet.Wallet.Persistence.Migrations;

[DbContext(typeof(WalletDbContext))]
[Migration("20260909211000_InitialWalletRegistry")]
public partial class InitialWalletRegistry : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Wallets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                OwnerId = table.Column<Guid>(type: "TEXT", nullable: false),
                CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                CountryCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Wallets", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Wallets_OwnerId_CurrencyCode",
            table: "Wallets",
            columns: new[] { "OwnerId", "CurrencyCode" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Wallets");
    }
}
