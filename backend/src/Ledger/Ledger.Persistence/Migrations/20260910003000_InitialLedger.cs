using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AfriWallet.Ledger.Persistence.Migrations;

[DbContext(typeof(LedgerDbContext))]
[Migration("20260910003000_InitialLedger")]
public partial class InitialLedger : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LedgerJournalEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                BusinessReference = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                CorrelationId = table.Column<Guid>(type: "TEXT", nullable: false),
                PostedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_LedgerJournalEntries", x => x.Id));

        migrationBuilder.CreateTable(
            name: "LedgerLines",
            columns: table => new
            {
                JournalEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                Position = table.Column<int>(type: "INTEGER", nullable: false),
                AccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                Side = table.Column<int>(type: "INTEGER", nullable: false),
                AmountMinor = table.Column<long>(type: "INTEGER", nullable: false),
                Memo = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LedgerLines", x => new { x.JournalEntryId, x.Position });
                table.ForeignKey(
                    name: "FK_LedgerLines_LedgerJournalEntries_JournalEntryId",
                    column: x => x.JournalEntryId,
                    principalTable: "LedgerJournalEntries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LedgerJournalEntries_CorrelationId",
            table: "LedgerJournalEntries",
            column: "CorrelationId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_LedgerLines_AccountId",
            table: "LedgerLines",
            column: "AccountId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LedgerLines");
        migrationBuilder.DropTable(name: "LedgerJournalEntries");
    }
}
