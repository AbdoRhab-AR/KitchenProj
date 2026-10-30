using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitchenProject.Migrations
{
    /// <inheritdoc />
    public partial class FixIdentityUserAndSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Contracts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Age",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "6068e8a9-d170-4fa6-abba-bed99e9ef5da",
                column: "CreatedDate",
                value: new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Contact",
                keyColumn: "Id",
                keyValue: new Guid("4a749a24-16a2-4e83-9b41-eafbb66aacec"),
                column: "Created",
                value: new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "SiteInfo",
                keyColumn: "Id",
                keyValue: new Guid("44b47e21-a6b8-48d3-b84c-8473ad5806e8"),
                column: "Created",
                value: new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "SiteState",
                keyColumn: "Id",
                keyValue: new Guid("7b2c5bf8-20f1-4956-abb6-e8821e8ce725"),
                column: "Created",
                value: new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "6068e8a9-d170-4fa6-abba-bed99e9ef5da");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Contracts",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "NEWID()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "Age",
                table: "AspNetUsers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "AspNetUsers",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "Age", "Approval", "ConcurrencyStamp", "CreatedDate", "Discriminator", "Email", "EmailConfirmed", "LastAccessTime", "LockoutEnabled", "LockoutEnd", "ModifiedDate", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "6068e8a9-d170-4fa6-abba-bed99e9ef5da", 0, 0, null, "50286ee0-8cab-4fd9-b86d-4c9e7d81d85b", new DateTime(2026, 9, 5, 13, 1, 21, 489, DateTimeKind.Utc).AddTicks(5206), "ApplicationUser", "Programmer@Gmail.com", true, null, true, null, null, "PROGRAMMER@GMAIL.COM", "PROGRAMMER@GMAIL.COM", "AQAAAAIAAYagAAAAEOiMVW5LwaXnP8KCeBDAECPBrxqGZdZDi2qpMwjKPFc0X+YlPL+CABdS42ebB7WrSw==", null, true, "04500de6-96fe-4ad7-96dd-fcca96e369d7", false, "Programmer@Gmail.com" });

            migrationBuilder.UpdateData(
                table: "Contact",
                keyColumn: "Id",
                keyValue: new Guid("4a749a24-16a2-4e83-9b41-eafbb66aacec"),
                column: "Created",
                value: new DateTime(2026, 9, 5, 15, 1, 21, 489, DateTimeKind.Local).AddTicks(4887));

            migrationBuilder.UpdateData(
                table: "SiteInfo",
                keyColumn: "Id",
                keyValue: new Guid("44b47e21-a6b8-48d3-b84c-8473ad5806e8"),
                column: "Created",
                value: new DateTime(2026, 9, 5, 15, 1, 21, 489, DateTimeKind.Local).AddTicks(4589));

            migrationBuilder.UpdateData(
                table: "SiteState",
                keyColumn: "Id",
                keyValue: new Guid("7b2c5bf8-20f1-4956-abb6-e8821e8ce725"),
                column: "Created",
                value: new DateTime(2026, 9, 5, 15, 1, 21, 489, DateTimeKind.Local).AddTicks(4910));
        }
    }
}
