using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitchenProject.Migrations
{
    /// <inheritdoc />
    public partial class Phase2QuotesContractsBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LetterheadUrl",
                table: "SiteInfo",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "Quotes",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "QuoteId",
                table: "Contracts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Specifications",
                table: "Contracts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "QuoteItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    QuoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteItems_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Contact",
                keyColumn: "Id",
                keyValue: new Guid("4a749a24-16a2-4e83-9b41-eafbb66aacec"),
                columns: new[] { "Email", "Facebook", "Instagram", "Phone", "X" },
                values: new object[] { "info@bengsoma.ly", "", "", "00218910997500", "" });

            migrationBuilder.UpdateData(
                table: "SiteInfo",
                keyColumn: "Id",
                keyValue: new Guid("44b47e21-a6b8-48d3-b84c-8473ad5806e8"),
                columns: new[] { "About", "Activity", "LetterheadUrl", "LogoUrl", "Name" },
                values: new object[] { "شركة بن قسومة لاستيراد الأثاث المنزلي والمكتبي وحلول المطابخ - السراج، طرابلس.", "Bin Gsoma Kitchen Solutions Company", "/images/brand/letterhead.png", "/images/brand/logo.png", "بن قسومة لحلول المطابخ" });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_QuoteId",
                table: "Contracts",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_QuoteId",
                table: "QuoteItems",
                column: "QuoteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Quotes_QuoteId",
                table: "Contracts",
                column: "QuoteId",
                principalTable: "Quotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Quotes_QuoteId",
                table: "Contracts");

            migrationBuilder.DropTable(
                name: "QuoteItems");

            migrationBuilder.DropIndex(
                name: "IX_Contracts_QuoteId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "LetterheadUrl",
                table: "SiteInfo");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "QuoteId",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "Specifications",
                table: "Contracts");

            migrationBuilder.UpdateData(
                table: "Contact",
                keyColumn: "Id",
                keyValue: new Guid("4a749a24-16a2-4e83-9b41-eafbb66aacec"),
                columns: new[] { "Email", "Facebook", "Instagram", "Phone", "X" },
                values: new object[] { "W.Wide@Gmail.com", "Worldwide Facebook", "Worldwide Instagram", "00218951234567", "Worldwide Twitter" });

            migrationBuilder.UpdateData(
                table: "SiteInfo",
                keyColumn: "Id",
                keyValue: new Guid("44b47e21-a6b8-48d3-b84c-8473ad5806e8"),
                columns: new[] { "About", "Activity", "LogoUrl", "Name" },
                values: new object[] { "We are a specialized news website covering political, sports, and economic events, along with various other sections of general interest to readers. We always strive to provide distinguished and reliable content that reflects the ongoing developments on both the local and global stages.", "News site", null, "بن قسومة للمطابخ" });
        }
    }
}
