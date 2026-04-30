using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaasApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScopesToOAuthClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Scopes",
                table: "OAuthClients",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            // Backfill existing clients with all scopes so they keep working.
            // New clients must explicitly choose scopes via the dashboard.
            migrationBuilder.Sql(
                "UPDATE [OAuthClients] SET [Scopes] = 'products:read,products:write,orders:read,orders:write,customers:read,dashboard:read'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scopes",
                table: "OAuthClients");
        }
    }
}
