using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaasApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DemoExpiresAt",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDemo",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DemoExpiresAt",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDemo",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_DemoExpiresAt",
                table: "Tenants",
                column: "DemoExpiresAt",
                filter: "[IsDemo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_DemoExpiresAt",
                table: "Customers",
                column: "DemoExpiresAt",
                filter: "[IsDemo] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_DemoExpiresAt",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Customers_DemoExpiresAt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DemoExpiresAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsDemo",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DemoExpiresAt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsDemo",
                table: "Customers");
        }
    }
}
