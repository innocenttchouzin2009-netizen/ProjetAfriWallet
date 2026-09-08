using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace IdentityService.Api.Auth.Persistence.Migrations;

[DbContext(typeof(AuthDbContext))]
[Migration("20260908181500_InitialAuthPersistence")]
public partial class InitialAuthPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "auth_users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                NormalizedIdentifier = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                PasswordHash = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                IsDisabled = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_auth_users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "auth_sessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                DeviceId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                RefreshTokenHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                RefreshTokenFamilyId = table.Column<Guid>(type: "TEXT", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                LastSeenAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                RevokedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                RevocationReason = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                TokenVersion = table.Column<long>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_auth_sessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_auth_sessions_auth_users_UserId",
                    column: x => x.UserId,
                    principalTable: "auth_users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "auth_consumed_refresh_tokens",
            columns: table => new
            {
                RefreshTokenHash = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                ConsumedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_auth_consumed_refresh_tokens", x => x.RefreshTokenHash);
                table.ForeignKey(
                    name: "FK_auth_consumed_refresh_tokens_auth_sessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "auth_sessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_auth_users_NormalizedIdentifier",
            table: "auth_users",
            column: "NormalizedIdentifier",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_auth_sessions_RefreshTokenFamilyId",
            table: "auth_sessions",
            column: "RefreshTokenFamilyId");

        migrationBuilder.CreateIndex(
            name: "IX_auth_sessions_RefreshTokenHash",
            table: "auth_sessions",
            column: "RefreshTokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_auth_sessions_Status",
            table: "auth_sessions",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_auth_sessions_UserId",
            table: "auth_sessions",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_auth_consumed_refresh_tokens_SessionId",
            table: "auth_consumed_refresh_tokens",
            column: "SessionId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "auth_consumed_refresh_tokens");
        migrationBuilder.DropTable(name: "auth_sessions");
        migrationBuilder.DropTable(name: "auth_users");
    }
}
