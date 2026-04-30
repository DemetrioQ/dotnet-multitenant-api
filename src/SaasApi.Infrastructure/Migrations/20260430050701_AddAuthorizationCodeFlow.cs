using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaasApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizationCodeFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ClientSecretHash",
                table: "OAuthClients",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<int>(
                name: "ClientType",
                table: "OAuthClients",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RedirectUris",
                table: "OAuthClients",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "OAuthAuthorizationCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OAuthClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RedirectUri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CodeChallenge = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CodeChallengeMethod = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Scopes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthAuthorizationCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OAuthRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OAuthClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scopes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthRefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OAuthAuthorizationCodes_Code",
                table: "OAuthAuthorizationCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuthAuthorizationCodes_ExpiresAt",
                table: "OAuthAuthorizationCodes",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthRefreshTokens_OAuthClientId",
                table: "OAuthRefreshTokens",
                column: "OAuthClientId");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthRefreshTokens_Token",
                table: "OAuthRefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuthRefreshTokens_UserId",
                table: "OAuthRefreshTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OAuthAuthorizationCodes");

            migrationBuilder.DropTable(
                name: "OAuthRefreshTokens");

            migrationBuilder.DropColumn(
                name: "ClientType",
                table: "OAuthClients");

            migrationBuilder.DropColumn(
                name: "RedirectUris",
                table: "OAuthClients");

            migrationBuilder.AlterColumn<string>(
                name: "ClientSecretHash",
                table: "OAuthClients",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
