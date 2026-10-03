using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitchenProject.Migrations
{
    /// <inheritdoc />
    public partial class AddMeasurementItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeasurementItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    SiteVisitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AreaName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Length = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Width = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Height = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeasurementItems_SiteVisits_SiteVisitId",
                        column: x => x.SiteVisitId,
                        principalTable: "SiteVisits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementItems_SiteVisitId",
                table: "MeasurementItems",
                column: "SiteVisitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeasurementItems");

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "23c776f9-799a-4fc2-81da-85d90fd223bc", "34b01874-616c-4392-b07e-2f53f310dddb" });

            migrationBuilder.DeleteData(
                table: "Contact",
                keyColumn: "Id",
                keyValue: new Guid("494e9bc6-aed9-469d-bcdf-6c1b89385836"));

            migrationBuilder.DeleteData(
                table: "SiteInfo",
                keyColumn: "Id",
                keyValue: new Guid("71d4c7b9-23b9-4d44-b443-893fb4e3e077"));

            migrationBuilder.DeleteData(
                table: "SiteState",
                keyColumn: "Id",
                keyValue: new Guid("0275f8f1-d793-4e8a-acde-cbca3cd0deae"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "23c776f9-799a-4fc2-81da-85d90fd223bc");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "34b01874-616c-4392-b07e-2f53f310dddb");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "109400b7-ee44-4b6c-bef8-0cc59de1543e", "43dccabf-992f-42c2-8ea0-e023ce5ad8dc", "Prog", "PROG" });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "Age", "Approval", "ConcurrencyStamp", "CreatedDate", "Discriminator", "Email", "EmailConfirmed", "LastAccessTime", "LockoutEnabled", "LockoutEnd", "ModifiedDate", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "6068e8a9-d170-4fa6-abba-bed99e9ef5da", 0, 0, null, "50286ee0-8cab-4fd9-b86d-4c9e7d81d85b", new DateTime(2026, 9, 5, 13, 1, 21, 489, DateTimeKind.Utc).AddTicks(5206), "ApplicationUser", "Programmer@Gmail.com", true, null, true, null, null, "PROGRAMMER@GMAIL.COM", "PROGRAMMER@GMAIL.COM", "AQAAAAIAAYagAAAAEOiMVW5LwaXnP8KCeBDAECPBrxqGZdZDi2qpMwjKPFc0X+YlPL+CABdS42ebB7WrSw==", null, true, "04500de6-96fe-4ad7-96dd-fcca96e369d7", false, "Programmer@Gmail.com" });

            migrationBuilder.InsertData(
                table: "Contact",
                columns: new[] { "Id", "Created", "Email", "Facebook", "Instagram", "Modified", "Phone", "X" },
                values: new object[] { new Guid("4a749a24-16a2-4e83-9b41-eafbb66aacec"), new DateTime(2026, 9, 5, 15, 1, 21, 489, DateTimeKind.Local).AddTicks(4887), "W.Wide@Gmail.com", "Worldwide Facebook", "Worldwide Instagram", null, "00218951234567", "Worldwide Twitter" });

            migrationBuilder.InsertData(
                table: "SiteInfo",
                columns: new[] { "Id", "About", "Activity", "CoverImageUrl", "Created", "LogoUrl", "Modified", "Name" },
                values: new object[] { new Guid("44b47e21-a6b8-48d3-b84c-8473ad5806e8"), "We are a specialized news website covering political, sports, and economic events, along with various other sections of general interest to readers. We always strive to provide distinguished and reliable content that reflects the ongoing developments on both the local and global stages.", "News site", null, new DateTime(2026, 9, 5, 15, 1, 21, 489, DateTimeKind.Local).AddTicks(4589), null, null, "بن قسومة للمطابخ" });

            migrationBuilder.InsertData(
                table: "SiteState",
                columns: new[] { "Id", "ClosingMessage", "Created", "Modified", "State" },
                values: new object[] { new Guid("7b2c5bf8-20f1-4956-abb6-e8821e8ce725"), "The site is temporarily closed for development", new DateTime(2026, 9, 5, 15, 1, 21, 489, DateTimeKind.Local).AddTicks(4910), null, true });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "109400b7-ee44-4b6c-bef8-0cc59de1543e", "6068e8a9-d170-4fa6-abba-bed99e9ef5da" });
        }
    }
}
