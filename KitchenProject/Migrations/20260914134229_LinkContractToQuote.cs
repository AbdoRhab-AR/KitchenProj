using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitchenProject.Migrations
{
    /// <inheritdoc />
    public partial class LinkContractToQuote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.AddColumn<Guid>(
                name: "QuoteId",
                table: "Contracts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));



            migrationBuilder.CreateIndex(
                name: "IX_Contracts_QuoteId",
                table: "Contracts",
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

            migrationBuilder.DropIndex(
                name: "IX_Contracts_QuoteId",
                table: "Contracts");

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "f0385e12-581d-4525-8383-0631b953ac2b", "80a1388b-50fc-49bb-b164-47b2d779bb91" });

            migrationBuilder.DeleteData(
                table: "Contact",
                keyColumn: "Id",
                keyValue: new Guid("0e5ffe0c-98b4-4b67-8f03-deca70a0eaef"));

            migrationBuilder.DeleteData(
                table: "SiteInfo",
                keyColumn: "Id",
                keyValue: new Guid("f3a35b3c-2922-42dc-ba88-87cf50bee927"));

            migrationBuilder.DeleteData(
                table: "SiteState",
                keyColumn: "Id",
                keyValue: new Guid("53ecb3e8-1486-4e8f-9035-f17d2aa5e0de"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "f0385e12-581d-4525-8383-0631b953ac2b");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "80a1388b-50fc-49bb-b164-47b2d779bb91");

            migrationBuilder.DropColumn(
                name: "QuoteId",
                table: "Contracts");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "23c776f9-799a-4fc2-81da-85d90fd223bc", "5dadb6d9-4dd0-4c5b-a0eb-a6d333f4c5fb", "Prog", "PROG" });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "Age", "Approval", "ConcurrencyStamp", "CreatedDate", "Discriminator", "Email", "EmailConfirmed", "LastAccessTime", "LockoutEnabled", "LockoutEnd", "ModifiedDate", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "34b01874-616c-4392-b07e-2f53f310dddb", 0, 0, null, "a125d061-ee04-4f21-b08e-cdf7c3e8b26b", new DateTime(2026, 9, 13, 12, 42, 14, 741, DateTimeKind.Utc).AddTicks(6507), "ApplicationUser", "Programmer@Gmail.com", true, null, true, null, null, "PROGRAMMER@GMAIL.COM", "PROGRAMMER@GMAIL.COM", "AQAAAAIAAYagAAAAEFXz+QgZo/M4VqC+StbhqJCa92l99u85lXMkRPMbyOIcEYNu54+KOLpnUfs4uDtpqQ==", null, true, "e4332622-b206-41ef-81dc-cb8f91555c46", false, "Programmer@Gmail.com" });

            migrationBuilder.InsertData(
                table: "Contact",
                columns: new[] { "Id", "Created", "Email", "Facebook", "Instagram", "Modified", "Phone", "X" },
                values: new object[] { new Guid("494e9bc6-aed9-469d-bcdf-6c1b89385836"), new DateTime(2026, 9, 13, 14, 42, 14, 741, DateTimeKind.Local).AddTicks(5796), "W.Wide@Gmail.com", "Worldwide Facebook", "Worldwide Instagram", null, "00218951234567", "Worldwide Twitter" });

            migrationBuilder.InsertData(
                table: "SiteInfo",
                columns: new[] { "Id", "About", "Activity", "CoverImageUrl", "Created", "LogoUrl", "Modified", "Name" },
                values: new object[] { new Guid("71d4c7b9-23b9-4d44-b443-893fb4e3e077"), "We are a specialized news website covering political, sports, and economic events, along with various other sections of general interest to readers. We always strive to provide distinguished and reliable content that reflects the ongoing developments on both the local and global stages.", "News site", null, new DateTime(2026, 9, 13, 14, 42, 14, 741, DateTimeKind.Local).AddTicks(4880), null, null, "بن قسومة للمطابخ" });

            migrationBuilder.InsertData(
                table: "SiteState",
                columns: new[] { "Id", "ClosingMessage", "Created", "Modified", "State" },
                values: new object[] { new Guid("0275f8f1-d793-4e8a-acde-cbca3cd0deae"), "The site is temporarily closed for development", new DateTime(2026, 9, 13, 14, 42, 14, 741, DateTimeKind.Local).AddTicks(5879), null, true });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "23c776f9-799a-4fc2-81da-85d90fd223bc", "34b01874-616c-4392-b07e-2f53f310dddb" });
        }
    }
}
