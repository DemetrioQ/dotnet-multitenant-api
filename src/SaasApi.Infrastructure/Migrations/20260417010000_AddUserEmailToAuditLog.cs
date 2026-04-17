using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaasApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEmailToAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                table: "AuditLogEntries",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "unknown");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "UserEmail", table: "AuditLogEntries");
        }
    }
}
