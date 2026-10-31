using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KitchenProject.Migrations
{
    /// <inheritdoc />
    public partial class ItemGroupNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ItemGroups",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-4000-8000-00000000000a"),
                columns: new[] { "Description", "Name" },
                values: new object[] { "الوحدات السفلية والعلوية والأعمدة", "وحدات المطبخ" });

            migrationBuilder.UpdateData(
                table: "ItemGroups",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-00000000000b"),
                columns: new[] { "Description", "Name" },
                values: new object[] { "أسطح الرخام والجرانيت والكوارتز", "الأسطح والرخام" });

            migrationBuilder.UpdateData(
                table: "ItemGroups",
                keyColumn: "Id",
                keyValue: new Guid("c1000000-0000-4000-8000-00000000000c"),
                columns: new[] { "Description", "Name" },
                values: new object[] { "الأجهزة الكهربائية والمغاسل والمقابض والإكسسوارات", "الأجهزة والإكسسوارات" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ItemGroups",
                keyColumn: "Id",
                keyValue: new Guid("a1000000-0000-4000-8000-00000000000a"),
                columns: new[] { "Description", "Name" },
                values: new object[] { null, "البند A" });

            migrationBuilder.UpdateData(
                table: "ItemGroups",
                keyColumn: "Id",
                keyValue: new Guid("b1000000-0000-4000-8000-00000000000b"),
                columns: new[] { "Description", "Name" },
                values: new object[] { null, "البند B" });

            migrationBuilder.UpdateData(
                table: "ItemGroups",
                keyColumn: "Id",
                keyValue: new Guid("c1000000-0000-4000-8000-00000000000c"),
                columns: new[] { "Description", "Name" },
                values: new object[] { null, "البند C" });
        }
    }
}
