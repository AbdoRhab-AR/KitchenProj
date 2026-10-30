using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KitchenProject.Migrations
{
    /// <inheritdoc />
    public partial class Phase3InventoryPurchasesPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Projects_ProjectId",
                table: "Purchases");

            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Suppliers_SupplierId",
                table: "Purchases");

            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Warehouses_WarehouseId",
                table: "Purchases");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_Suppliers_SupplierId",
                table: "SupplierPayments");

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "SupplierPayments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseId",
                table: "StockTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TransactionDate",
                table: "StockTransactions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "ItemId",
                table: "QuoteItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNo",
                table: "Purchases",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<bool>(
                name: "DirectToProject",
                table: "Purchases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseDate",
                table: "Purchases",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PurchaseNo",
                table: "Purchases",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ItemGroupId",
                table: "Items",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Items",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "CustomerPayments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNo",
                table: "CustomerPayments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ItemGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemGroups", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ItemGroups",
                columns: new[] { "Id", "Code", "Created", "Description", "Modified", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-4000-8000-00000000000a"), "A", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), null, null, "البند A", 1 },
                    { new Guid("b1000000-0000-4000-8000-00000000000b"), "B", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), null, null, "البند B", 2 },
                    { new Guid("c1000000-0000-4000-8000-00000000000c"), "C", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), null, null, "البند C", 3 }
                });

            migrationBuilder.InsertData(
                table: "Warehouses",
                columns: new[] { "Id", "Created", "IsActive", "Location", "ManagerId", "Modified", "Name" },
                values: new object[] { new Guid("d3f1c2a4-5b6e-4f70-8a91-0b2c3d4e5f61"), new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, null, null, null, "المخزن الرئيسي" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_PurchaseId",
                table: "SupplierPayments",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_PurchaseId",
                table: "StockTransactions",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_ItemId",
                table: "QuoteItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_Code",
                table: "Items",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemGroupId",
                table: "Items",
                column: "ItemGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemGroups_Code",
                table: "ItemGroups",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_ItemGroups_ItemGroupId",
                table: "Items",
                column: "ItemGroupId",
                principalTable: "ItemGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Projects_ProjectId",
                table: "Purchases",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Suppliers_SupplierId",
                table: "Purchases",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Warehouses_WarehouseId",
                table: "Purchases",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteItems_Items_ItemId",
                table: "QuoteItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransactions_Purchases_PurchaseId",
                table: "StockTransactions",
                column: "PurchaseId",
                principalTable: "Purchases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_Purchases_PurchaseId",
                table: "SupplierPayments",
                column: "PurchaseId",
                principalTable: "Purchases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_Suppliers_SupplierId",
                table: "SupplierPayments",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_ItemGroups_ItemGroupId",
                table: "Items");

            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Projects_ProjectId",
                table: "Purchases");

            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Suppliers_SupplierId",
                table: "Purchases");

            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Warehouses_WarehouseId",
                table: "Purchases");

            migrationBuilder.DropForeignKey(
                name: "FK_QuoteItems_Items_ItemId",
                table: "QuoteItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StockTransactions_Purchases_PurchaseId",
                table: "StockTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_Purchases_PurchaseId",
                table: "SupplierPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_Suppliers_SupplierId",
                table: "SupplierPayments");

            migrationBuilder.DropTable(
                name: "ItemGroups");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_PurchaseId",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_PurchaseId",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_QuoteItems_ItemId",
                table: "QuoteItems");

            migrationBuilder.DropIndex(
                name: "IX_Items_Code",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_ItemGroupId",
                table: "Items");

            migrationBuilder.DeleteData(
                table: "Warehouses",
                keyColumn: "Id",
                keyValue: new Guid("d3f1c2a4-5b6e-4f70-8a91-0b2c3d4e5f61"));

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "PurchaseId",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "TransactionDate",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "QuoteItems");

            migrationBuilder.DropColumn(
                name: "DirectToProject",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "PurchaseDate",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "PurchaseNo",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "ItemGroupId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "CustomerPayments");

            migrationBuilder.DropColumn(
                name: "ReferenceNo",
                table: "CustomerPayments");

            migrationBuilder.AlterColumn<string>(
                name: "InvoiceNo",
                table: "Purchases",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Projects_ProjectId",
                table: "Purchases",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Suppliers_SupplierId",
                table: "Purchases",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Warehouses_WarehouseId",
                table: "Purchases",
                column: "WarehouseId",
                principalTable: "Warehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_Suppliers_SupplierId",
                table: "SupplierPayments",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
