using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KitchenProject.Migrations
{
    /// <inheritdoc />
    public partial class RolesQuoteNotesTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultQuoteTerms",
                table: "SiteInfo",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "QuoteItems",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e01", "a7c1e3f5-1111-4a00-8000-000000000001", "Admin", "ADMIN" },
                    { "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e02", "a7c1e3f5-1111-4a00-8000-000000000002", "Sales", "SALES" },
                    { "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e03", "a7c1e3f5-1111-4a00-8000-000000000003", "Engineer", "ENGINEER" },
                    { "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e04", "a7c1e3f5-1111-4a00-8000-000000000004", "Designer", "DESIGNER" },
                    { "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e05", "a7c1e3f5-1111-4a00-8000-000000000005", "Warehouse", "WAREHOUSE" },
                    { "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e06", "a7c1e3f5-1111-4a00-8000-000000000006", "Accountant", "ACCOUNTANT" }
                });

            migrationBuilder.UpdateData(
                table: "SiteInfo",
                keyColumn: "Id",
                keyValue: new Guid("44b47e21-a6b8-48d3-b84c-8473ad5806e8"),
                column: "DefaultQuoteTerms",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e01");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e02");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e03");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e04");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e05");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e06");

            migrationBuilder.DropColumn(
                name: "DefaultQuoteTerms",
                table: "SiteInfo");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "QuoteItems");
        }
    }
}
