using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KitchenProject.Migrations
{
    /// <inheritdoc />
    public partial class @new : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsPostable = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SystemKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_Accounts_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    LastAccessTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Approval = table.Column<bool>(type: "bit", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contact",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Facebook = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    X = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Instagram = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    CustomerNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone2 = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

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

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    EntryNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsAuto = table.Column<bool>(type: "bit", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PeriodCloses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ClosedThrough = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NetResult = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ClosedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodCloses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteInfo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Activity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    About = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LetterheadUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultQuoteTerms = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PageSize = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteState",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    State = table.Column<bool>(type: "bit", nullable: false),
                    ClosingMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    SupplierNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PaymentTerms = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gender = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ProjectNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    KitchenType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SalesUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    EngineerUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DesignerUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EstimatedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FinalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CancellationFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_AspNetUsers_DesignerUserId",
                        column: x => x.DesignerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_AspNetUsers_EngineerUserId",
                        column: x => x.EngineerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_AspNetUsers_SalesUserId",
                        column: x => x.SalesUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ItemGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_ItemGroups_ItemGroupId",
                        column: x => x.ItemGroupId,
                        principalTable: "ItemGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    JournalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Debit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalLines_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalLines_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpenseType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Payment = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttachmentPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CashAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expenses_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    PurchaseNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DirectToProject = table.Column<bool>(type: "bit", nullable: false),
                    InvoiceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Purchases_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Purchases_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Purchases_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuoteNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SiteVisits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EngineerId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    VisitDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Length = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Width = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Height = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    MeasurementsText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteVisits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteVisits_AspNetUsers_EngineerId",
                        column: x => x.EngineerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteVisits_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    RequestNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NeededBy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DecidedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PurchaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IssueRef = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialRequests_AspNetUsers_DecidedById",
                        column: x => x.DecidedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialRequests_AspNetUsers_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialRequests_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialRequests_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MaterialRequests_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    PurchaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseItems_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ReturnNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReturns_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReturns_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReturns_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiptNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CashAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChequeStatus = table.Column<int>(type: "int", nullable: true),
                    ChequeDueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPrinted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierPayments_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierPayments_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContractNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Specifications = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ContractFilePath = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contracts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contracts_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuoteItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    QuoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
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
                        name: "FK_QuoteItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QuoteItems_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialRequestItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    MaterialRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialRequestItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialRequestItems_MaterialRequests_MaterialRequestId",
                        column: x => x.MaterialRequestId,
                        principalTable: "MaterialRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReturnItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    PurchaseReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReturnItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnItems_PurchaseReturns_PurchaseReturnId",
                        column: x => x.PurchaseReturnId,
                        principalTable: "PurchaseReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurchaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurchaseReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuantityIn = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantityOut = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransactions_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_PurchaseReturns_PurchaseReturnId",
                        column: x => x.PurchaseReturnId,
                        principalTable: "PurchaseReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockTransactions_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiptNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Payment = table.Column<int>(type: "int", nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRefund = table.Column<bool>(type: "bit", nullable: false),
                    CashAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChequeStatus = table.Column<int>(type: "int", nullable: true),
                    ChequeDueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPrinted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Modified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerPayments_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerPayments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "109400b7-ee44-4b6c-bef8-0cc59de1543e", "43dccabf-992f-42c2-8ea0-e023ce5ad8dc", "Prog", "PROG" },
                    { "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e01", "a7c1e3f5-1111-4a00-8000-000000000001", "Admin", "ADMIN" },
                    { "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e02", "a7c1e3f5-1111-4a00-8000-000000000002", "Sales", "SALES" },
                    { "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e03", "a7c1e3f5-1111-4a00-8000-000000000003", "Engineer", "ENGINEER" },
                    { "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e04", "a7c1e3f5-1111-4a00-8000-000000000004", "Designer", "DESIGNER" },
                    { "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e05", "a7c1e3f5-1111-4a00-8000-000000000005", "Warehouse", "WAREHOUSE" },
                    { "2f1d6c3a-0b7e-4a51-9d0e-1a2b3c4d5e06", "a7c1e3f5-1111-4a00-8000-000000000006", "Accountant", "ACCOUNTANT" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "Age", "Approval", "ConcurrencyStamp", "CreatedDate", "Email", "EmailConfirmed", "LastAccessTime", "LockoutEnabled", "LockoutEnd", "ModifiedDate", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "6068e8a9-d170-4fa6-abba-bed99e9ef5da", 0, 0, null, "50286ee0-8cab-4fd9-b86d-4c9e7d81d85b", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), "Programmer@Gmail.com", true, null, true, null, null, "PROGRAMMER@GMAIL.COM", "PROGRAMMER@GMAIL.COM", "AQAAAAIAAYagAAAAEOiMVW5LwaXnP8KCeBDAECPBrxqGZdZDi2qpMwjKPFc0X+YlPL+CABdS42ebB7WrSw==", null, true, "04500de6-96fe-4ad7-96dd-fcca96e369d7", false, "Programmer@Gmail.com" });

            migrationBuilder.InsertData(
                table: "Contact",
                columns: new[] { "Id", "Created", "Email", "Facebook", "Instagram", "Modified", "Phone", "X" },
                values: new object[] { new Guid("4a749a24-16a2-4e83-9b41-eafbb66aacec"), new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), "info@bengsoma.ly", "", "", null, "00218910997500", "" });

            migrationBuilder.InsertData(
                table: "ItemGroups",
                columns: new[] { "Id", "Code", "Created", "Description", "Modified", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-4000-8000-00000000000a"), "A1", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), "دواليب المطبخ خشب سوبر مات تركي MDF مع إكسسوارات بلوم نمساوية", null, "وحدات مطبخ - سوبر مات تركي MDF", 1 },
                    { new Guid("a2000000-0000-4000-8000-000000000002"), "A2", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), "دواليب المطبخ خشب هاي غلوس تركي MDF مع إكسسوارات بلوم نمساوية", null, "وحدات مطبخ - هاي غلوس تركي MDF", 2 },
                    { new Guid("a3000000-0000-4000-8000-000000000003"), "A3", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), "تلبيس الحوائط والعلب والأرفف الخشبية وأجناب التلبيس", null, "التلبيس والأرفف والأجناب", 3 },
                    { new Guid("b1000000-0000-4000-8000-00000000000b"), "B", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), "أسطح الرخام والجرانيت والكوارتز", null, "الأسطح والرخام", 4 },
                    { new Guid("c1000000-0000-4000-8000-00000000000c"), "C1", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), "أدراج بلوم ليقرا وانتارو وميريفو وموفينتو وتاندم", null, "أدراج بلوم النمساوية", 5 },
                    { new Guid("c2000000-0000-4000-8000-000000000002"), "C2", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), "آليات رفع الأبواب أفانتوس HF و HS و HL و HK بلوم", null, "قلابات أفانتوس بلوم", 6 },
                    { new Guid("c3000000-0000-4000-8000-000000000003"), "C3", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), "تقسيم الملاعق وحاملات الصحون والبهارات ومحركات سيرفودرايف", null, "تقسيمات الأدراج وسيرفودرايف", 7 },
                    { new Guid("c4000000-0000-4000-8000-000000000004"), "C4", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), "أدراج سامت وسلال التوابل والزاوية والسقطار والإضاءة والأجهزة", null, "إكسسوارات وسلال وإضاءة LED", 8 },
                    { new Guid("d1000000-0000-4000-8000-000000000001"), "D", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), "خدمات النقل والمناولة والتركيب وطاولات العمل", null, "النقل والتركيب والتجهيزات", 9 }
                });

            migrationBuilder.InsertData(
                table: "SiteInfo",
                columns: new[] { "Id", "About", "Activity", "CoverImageUrl", "Created", "DefaultQuoteTerms", "LetterheadUrl", "LogoUrl", "Modified", "Name", "PageSize" },
                values: new object[] { new Guid("44b47e21-a6b8-48d3-b84c-8473ad5806e8"), "شركة بن قسومة لاستيراد الأثاث المنزلي والمكتبي وحلول المطابخ - السراج، طرابلس.", "Bin Gsoma Kitchen Solutions Company", null, new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), null, "/images/brand/letterhead.png", "/images/brand/logo.png", null, "بن قسومة لحلول المطابخ", 10 });

            migrationBuilder.InsertData(
                table: "SiteState",
                columns: new[] { "Id", "ClosingMessage", "Created", "Modified", "State" },
                values: new object[] { new Guid("7b2c5bf8-20f1-4956-abb6-e8821e8ce725"), "The site is temporarily closed for development", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), null, true });

            migrationBuilder.InsertData(
                table: "Warehouses",
                columns: new[] { "Id", "Created", "IsActive", "Location", "ManagerId", "Modified", "Name" },
                values: new object[] { new Guid("d3f1c2a4-5b6e-4f70-8a91-0b2c3d4e5f61"), new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, null, null, null, "المخزن الرئيسي" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "109400b7-ee44-4b6c-bef8-0cc59de1543e", "6068e8a9-d170-4fa6-abba-bed99e9ef5da" });

            migrationBuilder.InsertData(
                table: "Items",
                columns: new[] { "Id", "Category", "Code", "Created", "IsActive", "ItemGroupId", "MinQuantity", "Modified", "Name", "Notes", "PurchasePrice", "SalePrice", "Type", "Unit" },
                values: new object[,]
                {
                    { new Guid("f0000000-0000-4000-8000-000000000001"), "سوبر مات تركي MDF", "FG-0001", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a1000000-0000-4000-8000-00000000000a"), 0m, null, "دولاب مطبخ علوي عمق 35 سم وأرتفاع 72 سم (سوبر مات)", null, 0m, 1250m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000002"), "سوبر مات تركي MDF", "FG-0002", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a1000000-0000-4000-8000-00000000000a"), 0m, null, "دولاب مطبخ سفلي عمق 60 سم وأرتفاع 87.5 سم (سوبر مات)", null, 0m, 1500m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000003"), "سوبر مات تركي MDF", "FG-0003", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a1000000-0000-4000-8000-00000000000a"), 0m, null, "دولاب مطبخ الجزء العلوي المعلق عمق 60 سم وأرتفاع 60 سم (سوبر مات)", null, 0m, 1450m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000004"), "سوبر مات تركي MDF", "FG-0004", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a1000000-0000-4000-8000-00000000000a"), 0m, null, "دولاب مطبخ سفلي (بار) عمق 60 سم وأرتفاع 87.5 سم (سوبر مات)", null, 0m, 1500m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000005"), "سوبر مات تركي MDF", "FG-0005", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a1000000-0000-4000-8000-00000000000a"), 0m, null, "دولاب مطبخ الجزء الكامل عمق 60 سم وأرتفاع 219 سم (سوبر مات)", null, 0m, 3625m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000006"), "سوبر مات تركي MDF", "FG-0006", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a1000000-0000-4000-8000-00000000000a"), 0m, null, "دولاب مطبخ علوي فوق الثلاجة عمق 60 سم (سوبر مات)", "الارتفاع = (حسب ارتفاع الثلاجة) + 3 سم، العرض = (عرض الثلاجة + 5 سم أو 7 سم)", 0m, 1450m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000007"), "سوبر مات تركي MDF", "FG-0007", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a1000000-0000-4000-8000-00000000000a"), 0m, null, "دولاب مطبخ الجزء العلوي 35 سم وأرتفاع 72 سم (زجاج محلي) (سوبر مات)", null, 0m, 1300m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000008"), "سوبر مات تركي MDF", "FG-0008", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a1000000-0000-4000-8000-00000000000a"), 0m, null, "دولاب مطبخ سفلي عمق 60 سم وأرتفاع 87.5 سم (زجاج محلي) (سوبر مات)", null, 0m, 1500m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000009"), "سوبر مات تركي MDF", "FG-0009", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a1000000-0000-4000-8000-00000000000a"), 0m, null, "دولاب مطبخ الجزء العلوي المعلق عمق 60 سم وأرتفاع 60 سم (زجاج محلي) (سوبر مات)", null, 0m, 1500m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000010"), "سوبر مات تركي MDF", "FG-0010", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a1000000-0000-4000-8000-00000000000a"), 0m, null, "دولاب مطبخ الجزء الكامل عمق 60 سم وأرتفاع 219 (زجاج محلي) (سوبر مات)", null, 0m, 3865m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000011"), "سوبر مات تركي MDF", "FG-0011", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a1000000-0000-4000-8000-00000000000a"), 0m, null, "دولاب مطبخ الجزء الكامل زجاج من جهتين عمق 60 سم وأرتفاع 219 (زجاج محلي) (سوبر مات)", null, 0m, 4600m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000012"), "سوبر مات تركي MDF", "FG-0012", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a1000000-0000-4000-8000-00000000000a"), 0m, null, "دولاب مطبخ الجزء العلوي 35 سم وأرتفاع 72 سم (زجاج بلجيكي) (سوبر مات)", null, 0m, 1525m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000013"), "سوبر مات تركي MDF", "FG-0013", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a1000000-0000-4000-8000-00000000000a"), 0m, null, "دولاب مطبخ سفلي عمق 60 سم وأرتفاع 87.5 سم (زجاج بلجيكي) (سوبر مات)", null, 0m, 1750m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000014"), "سوبر مات تركي MDF", "FG-0014", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a1000000-0000-4000-8000-00000000000a"), 0m, null, "دولاب مطبخ الجزء العلوي المعلق عمق 60 سم وأرتفاع 60 سم (زجاج بلجيكي) (سوبر مات)", null, 0m, 1700m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000015"), "سوبر مات تركي MDF", "FG-0015", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a1000000-0000-4000-8000-00000000000a"), 0m, null, "دولاب مطبخ الجزء الكامل عمق 60 سم وأرتفاع 219 (زجاج بلجيكي) (سوبر مات)", null, 0m, 4475m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000016"), "سوبر مات تركي MDF", "FG-0016", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a1000000-0000-4000-8000-00000000000a"), 0m, null, "دولاب مطبخ الجزء الكامل زجاج من جهتين عمق 60 سم وأرتفاع 219 (زجاج بلجيكي) (سوبر مات)", null, 0m, 5100m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000017"), "هاي غلوس تركي MDF", "FG-0017", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a2000000-0000-4000-8000-000000000002"), 0m, null, "دولاب مطبخ علوي عمق 35 سم وأرتفاع 72 سم (هاي غلوس)", null, 0m, 1000m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000018"), "هاي غلوس تركي MDF", "FG-0018", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a2000000-0000-4000-8000-000000000002"), 0m, null, "دولاب مطبخ سفلي عمق 60 سم وأرتفاع 87.5 سم (هاي غلوس)", null, 0m, 1200m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000019"), "هاي غلوس تركي MDF", "FG-0019", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a2000000-0000-4000-8000-000000000002"), 0m, null, "دولاب مطبخ الجزء العلوي المعلق عمق 60 سم وأرتفاع 60 سم (هاي غلوس)", null, 0m, 1000m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000020"), "هاي غلوس تركي MDF", "FG-0020", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a2000000-0000-4000-8000-000000000002"), 0m, null, "دولاب مطبخ سفلي (بار) عمق 60 سم وأرتفاع 87.5 سم (هاي غلوس)", null, 0m, 1200m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000021"), "هاي غلوس تركي MDF", "FG-0021", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a2000000-0000-4000-8000-000000000002"), 0m, null, "دولاب مطبخ الجزء الكامل عمق 60 سم وأرتفاع 219 سم (هاي غلوس)", null, 0m, 3100m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000022"), "هاي غلوس تركي MDF", "FG-0022", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a2000000-0000-4000-8000-000000000002"), 0m, null, "دولاب مطبخ علوي فوق الثلاجة عمق 60 سم (هاي غلوس)", "الارتفاع = (حسب ارتفاع الثلاجة) + 3 سم، العرض = (عرض الثلاجة + 5 سم أو 7 سم)", 0m, 1000m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000023"), "هاي غلوس تركي MDF", "FG-0023", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a2000000-0000-4000-8000-000000000002"), 0m, null, "دولاب مطبخ الجزء العلوي 35 سم وأرتفاع 72 سم (زجاج محلي) (هاي غلوس)", null, 0m, 1300m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000024"), "هاي غلوس تركي MDF", "FG-0024", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a2000000-0000-4000-8000-000000000002"), 0m, null, "دولاب مطبخ سفلي عمق 60 سم وأرتفاع 87.5 سم (زجاج محلي) (هاي غلوس)", null, 0m, 0m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000025"), "هاي غلوس تركي MDF", "FG-0025", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a2000000-0000-4000-8000-000000000002"), 0m, null, "دولاب مطبخ الجزء العلوي المعلق عمق 60 سم وأرتفاع 60 سم (زجاج محلي) (هاي غلوس)", null, 0m, 0m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000026"), "هاي غلوس تركي MDF", "FG-0026", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a2000000-0000-4000-8000-000000000002"), 0m, null, "دولاب مطبخ الجزء الكامل عمق 60 سم وأرتفاع 219 (زجاج محلي) (هاي غلوس)", null, 0m, 3740m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000027"), "هاي غلوس تركي MDF", "FG-0027", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a2000000-0000-4000-8000-000000000002"), 0m, null, "دولاب مطبخ الجزء الكامل زجاج من جهتين عمق 60 سم وأرتفاع 219 (زجاج محلي) (هاي غلوس)", null, 0m, 0m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000028"), "هاي غلوس تركي MDF", "FG-0028", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a2000000-0000-4000-8000-000000000002"), 0m, null, "دولاب مطبخ الجزء العلوي 35 سم وأرتفاع 72 سم (زجاج بلجيكي) (هاي غلوس)", null, 0m, 1300m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000029"), "هاي غلوس تركي MDF", "FG-0029", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a2000000-0000-4000-8000-000000000002"), 0m, null, "دولاب مطبخ سفلي عمق 60 سم وأرتفاع 87.5 سم (زجاج بلجيكي) (هاي غلوس)", null, 0m, 1400m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000030"), "هاي غلوس تركي MDF", "FG-0030", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a2000000-0000-4000-8000-000000000002"), 0m, null, "دولاب مطبخ الجزء العلوي المعلق عمق 60 سم وأرتفاع 60 سم (زجاج بلجيكي) (هاي غلوس)", null, 0m, 1200m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000031"), "هاي غلوس تركي MDF", "FG-0031", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a2000000-0000-4000-8000-000000000002"), 0m, null, "دولاب مطبخ الجزء الكامل عمق 60 سم وأرتفاع 219 (زجاج بلجيكي) (هاي غلوس)", null, 0m, 4475m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000032"), "هاي غلوس تركي MDF", "FG-0032", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a2000000-0000-4000-8000-000000000002"), 0m, null, "دولاب مطبخ الجزء الكامل زجاج من جهتين عمق 60 سم وأرتفاع 219 (زجاج بلجيكي) (هاي غلوس)", null, 0m, 5100m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000033"), "التلبيس والأرفف", "FG-0033", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a3000000-0000-4000-8000-000000000003"), 0m, null, "تلبيس خلفية شفاط", null, 0m, 0m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000034"), "التلبيس والأرفف", "FG-0034", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a3000000-0000-4000-8000-000000000003"), 0m, null, "تلبيس العلوي من أسفل عمق 35 سم", null, 0m, 450m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000035"), "التلبيس والأرفف", "FG-0035", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a3000000-0000-4000-8000-000000000003"), 0m, null, "تلبيس العلوي المعلق من أسفل عمق 60 سم", null, 0m, 450m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000036"), "التلبيس والأرفف", "FG-0036", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a3000000-0000-4000-8000-000000000003"), 0m, null, "تلبيس قرصة خشبية للجزء السفلي عمق 60 سم", null, 0m, 450m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000037"), "التلبيس والأرفف", "FG-0037", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a3000000-0000-4000-8000-000000000003"), 0m, null, "تلبيس 10 سم", null, 0m, 90m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000038"), "التلبيس والأرفف", "FG-0038", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a3000000-0000-4000-8000-000000000003"), 0m, null, "تلبيس حائط مودرن سمك 18 (المتر المربع)", null, 0m, 600m, 2, "متر مربع" },
                    { new Guid("f0000000-0000-4000-8000-000000000039"), "التلبيس والأرفف", "FG-0039", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a3000000-0000-4000-8000-000000000003"), 0m, null, "تلبيس حائط مودرن سمك 36 (المتر المربع)", null, 0m, 800m, 2, "متر مربع" },
                    { new Guid("f0000000-0000-4000-8000-000000000040"), "التلبيس والأرفف", "FG-0040", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a3000000-0000-4000-8000-000000000003"), 0m, null, "تلبيس حائط مودرن سمك 20 سم (المتر المربع)", null, 0m, 1250m, 2, "متر مربع" },
                    { new Guid("f0000000-0000-4000-8000-000000000041"), "التلبيس والأرفف", "FG-0041", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a3000000-0000-4000-8000-000000000003"), 0m, null, "ارفف خشبية (1.8 سم) عمق 20 سم", null, 0m, 450m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000042"), "التلبيس والأرفف", "FG-0042", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a3000000-0000-4000-8000-000000000003"), 0m, null, "ارفف خشبية دبل (3.6 سم) عمق 20 سم", null, 0m, 450m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000043"), "التلبيس والأرفف", "FG-0043", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a3000000-0000-4000-8000-000000000003"), 0m, null, "جنب تلبيس علوي عمق 35 سم وأرتفاع 72 سم", null, 0m, 450m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000044"), "التلبيس والأرفف", "FG-0044", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a3000000-0000-4000-8000-000000000003"), 0m, null, "جنب تلبيس سفلي عمق 60 سم وأرتفاع 72 سم", null, 0m, 500m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000045"), "التلبيس والأرفف", "FG-0045", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a3000000-0000-4000-8000-000000000003"), 0m, null, "جنب تلبيس كامل عمق 60 سم وأرتفاع 204 سم", null, 0m, 800m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000046"), "التلبيس والأرفف", "FG-0046", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("a3000000-0000-4000-8000-000000000003"), 0m, null, "جنب تلبيس عمق 60 سم وأرتفاع 132 سم", null, 0m, 600m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000047"), "أدراج بلوم", "FG-0047", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج ليقرا صغير M بلوم", null, 0m, 520m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000048"), "أدراج بلوم", "FG-0048", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج ليقرا كبير C بلوم", null, 0m, 0m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000049"), "أدراج بلوم", "FG-0049", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج ليقرا كبير F بلوم", null, 0m, 790m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000050"), "أدراج بلوم", "FG-0050", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج ليقرا M صغير TOB بلوم", null, 0m, 830m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000051"), "أدراج بلوم", "FG-0051", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج ليقرا C كبير TOB بلوم", null, 0m, 930m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000052"), "أدراج بلوم", "FG-0052", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج ليقرا F كبير TOB بلوم", null, 0m, 1000m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000053"), "أدراج بلوم", "FG-0053", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج انتارو M صغير بلوم", null, 0m, 440m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000054"), "أدراج بلوم", "FG-0054", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج انتارو D كبير بالماسورة بلوم", null, 0m, 600m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000055"), "أدراج بلوم", "FG-0055", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج انتارو D / U كبير تحت الحوض بالماسورة بلوم", "درج انتارو D+U", 0m, 650m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000056"), "أدراج بلوم", "FG-0056", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج انتارو M صغير TOB بلوم", null, 0m, 745m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000057"), "أدراج بلوم", "FG-0057", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج انتارو D كبير بالماسورة TOB بلوم", null, 0m, 780m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000058"), "أدراج بلوم", "FG-0058", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج ميريفو M صغير بلوم", null, 0m, 480m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000059"), "أدراج بلوم", "FG-0059", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج ميريفو E بالماسورة كبير بلوم", null, 0m, 550m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000060"), "أدراج بلوم", "FG-0060", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج ميريفو كبير E اجناب اضافية بلوم", null, 0m, 620m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000061"), "أدراج بلوم", "FG-0061", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج ميريفو M صغير TOB بلوم", null, 0m, 650m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000062"), "أدراج بلوم", "FG-0062", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج ميريفو E بالماسورة TOB بلوم", null, 0m, 720m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000063"), "أدراج بلوم", "FG-0063", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج ميريفو كبير E اجناب اضافية TOB بلوم", null, 0m, 800m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000064"), "أدراج بلوم", "FG-0064", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج ليقرا M بلوم انجليزي", null, 0m, 750m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000065"), "أدراج بلوم", "FG-0065", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج ليقرا C كبير بلوم انجليزي (سوبر مات)", null, 0m, 1200m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000066"), "أدراج بلوم", "FG-0066", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج ليقرا C كبير بلوم انجليزي (هاي غلوس)", null, 0m, 880m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000067"), "أدراج بلوم", "FG-0067", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج انتارو M صغير بلوم انجليزي", null, 0m, 600m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000068"), "أدراج بلوم", "FG-0068", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج انتارو D بالماسورة بلوم انجليزي (سوبر مات)", null, 0m, 600m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000069"), "أدراج بلوم", "FG-0069", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج انتارو D بالماسورة بلوم انجليزي (هاي غلوس)", null, 0m, 1200m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000070"), "أدراج بلوم", "FG-0070", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج موريفو E وجه ماسورة وجنب مواسير بلوم انجليزي", null, 0m, 800m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000071"), "أدراج بلوم", "FG-0071", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج موريفو E وجه ماسورة بالاجناب الاضافية معدنية بلوم انجليزي", null, 0m, 870m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000072"), "أدراج بلوم", "FG-0072", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج ليقرا M صغير بلوم انجليزي TOB", null, 0m, 930m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000073"), "أدراج بلوم", "FG-0073", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج ليقرا C كبير بلوم انجليزي TOB", null, 0m, 1200m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000074"), "أدراج بلوم", "FG-0074", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج انتارو M صغير بلوم انجليزي TOB", null, 0m, 780m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000075"), "أدراج بلوم", "FG-0075", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج انتارو D بالماسورة بلوم انجليزي TOB", null, 0m, 1200m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000076"), "أدراج بلوم", "FG-0076", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج موريفو E وجه ماسورة وجنب مواسير بلوم انجليزي TOB", null, 0m, 980m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000077"), "أدراج بلوم", "FG-0077", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج موريفو E وجه ماسورة بالاجناب الاضافية معدنية بلوم انجليزي TOB", null, 0m, 1050m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000078"), "أدراج بلوم", "FG-0078", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج موفينتو بلوم", null, 0m, 450m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000079"), "أدراج بلوم", "FG-0079", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج موفينتو TOB بلوم", null, 0m, 630m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000080"), "أدراج بلوم", "FG-0080", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج موفينتو رف متحرك بلوم", null, 0m, 580m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000081"), "أدراج بلوم", "FG-0081", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج موفينتو رف متحرك TOB بلوم", null, 0m, 630m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000082"), "أدراج بلوم", "FG-0082", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج تاندم بلومشن بلوم", null, 0m, 350m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000083"), "أدراج بلوم", "FG-0083", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج تاندم كبير بلوم", null, 0m, 350m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000084"), "أدراج بلوم", "FG-0084", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج تاندم TIP ON بلوم (سوبر مات)", null, 0m, 400m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000085"), "أدراج بلوم", "FG-0085", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c1000000-0000-4000-8000-00000000000c"), 0m, null, "درج تاندم TIP ON بلوم (هاي غلوس)", null, 0m, 450m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000086"), "أفانتوس بلوم", "FG-0086", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس 28 HF بلوم", null, 0m, 1400m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000087"), "أفانتوس بلوم", "FG-0087", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس 25 HF بلوم", null, 0m, 1300m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000088"), "أفانتوس بلوم", "FG-0088", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس 22 HF بلوم", null, 0m, 1200m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000089"), "أفانتوس بلوم", "FG-0089", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس I HS بلوم", null, 0m, 1000m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000090"), "أفانتوس بلوم", "FG-0090", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس H HS بلوم", null, 0m, 750m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000091"), "أفانتوس بلوم", "FG-0091", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس G HS بلوم", null, 0m, 750m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000092"), "أفانتوس بلوم", "FG-0092", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس 29 HL بلوم", null, 0m, 1100m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000093"), "أفانتوس بلوم", "FG-0093", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس 27 HL بلوم", null, 0m, 900m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000094"), "أفانتوس بلوم", "FG-0094", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس 25 HL بلوم", null, 0m, 835m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000095"), "أفانتوس بلوم", "FG-0095", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس HK TOP 25 بلوم", null, 0m, 580m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000096"), "أفانتوس بلوم", "FG-0096", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس HK TOP 27 بلوم", null, 0m, 580m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000097"), "أفانتوس بلوم", "FG-0097", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس HK TOP 29 بلوم", null, 0m, 640m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000098"), "أفانتوس بلوم", "FG-0098", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس HK TOP 25 Tip on بلوم", null, 0m, 635m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000099"), "أفانتوس بلوم", "FG-0099", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس HK TOP 27 Tip on بلوم", null, 0m, 660m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000100"), "أفانتوس بلوم", "FG-0100", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس HK TOP 29 Tip on بلوم", null, 0m, 720m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000101"), "أفانتوس بلوم", "FG-0101", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس E / HK-S بلوم", null, 0m, 350m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000102"), "أفانتوس بلوم", "FG-0102", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس HK-XS بلوم", null, 0m, 350m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000103"), "أفانتوس بلوم", "FG-0103", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس دبل HK-XS بلوم", null, 0m, 690m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000104"), "أفانتوس بلوم", "FG-0104", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس HK-XS Tip on بلوم", null, 0m, 380m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000105"), "أفانتوس بلوم", "FG-0105", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c2000000-0000-4000-8000-000000000002"), 0m, null, "افانتوس دبل HK-XS Tip on بلوم", null, 0m, 750m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000106"), "تقسيمات وسيرفودرايف", "FG-0106", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c3000000-0000-4000-8000-000000000003"), 0m, null, "تقسيم ملاعق 90", null, 0m, 1500m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000107"), "تقسيمات وسيرفودرايف", "FG-0107", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c3000000-0000-4000-8000-000000000003"), 0m, null, "تقسيم ملاعق 60", null, 0m, 100m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000108"), "تقسيمات وسيرفودرايف", "FG-0108", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c3000000-0000-4000-8000-000000000003"), 0m, null, "حاملة صحون", null, 0m, 400m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000109"), "تقسيمات وسيرفودرايف", "FG-0109", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c3000000-0000-4000-8000-000000000003"), 0m, null, "حاملة بهارات", null, 0m, 400m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000110"), "تقسيمات وسيرفودرايف", "FG-0110", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c3000000-0000-4000-8000-000000000003"), 0m, null, "قاطعة سلفر و اليمنيوم مزدوجة", null, 0m, 600m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000111"), "تقسيمات وسيرفودرايف", "FG-0111", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c3000000-0000-4000-8000-000000000003"), 0m, null, "ملحقات سيرفودرايف HS / HL", null, 0m, 4200m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000112"), "تقسيمات وسيرفودرايف", "FG-0112", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c3000000-0000-4000-8000-000000000003"), 0m, null, "ملحقات سيرفودرايف HF", null, 0m, 4100m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000113"), "تقسيمات وسيرفودرايف", "FG-0113", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c3000000-0000-4000-8000-000000000003"), 0m, null, "محرك درج أونو", null, 0m, 1600m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000114"), "تقسيمات وسيرفودرايف", "FG-0114", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c3000000-0000-4000-8000-000000000003"), 0m, null, "محرك سيرفو فليكس (ثلاجة وغسالة)", "محرك سيرفو فليكس (ثلاجة + غسالة)", 0m, 3000m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000115"), "إكسسوارات تركية وصينية", "FG-0115", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "درج سامت تركي صغير", null, 0m, 350m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000116"), "إكسسوارات تركية وصينية", "FG-0116", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "درج سامت تركي كبير", null, 0m, 400m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000117"), "إكسسوارات تركية وصينية", "FG-0117", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "قلاب مزطوري", null, 0m, 80m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000118"), "إكسسوارات تركية وصينية", "FG-0118", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "قلاب مزطوري دبل", null, 0m, 160m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000119"), "إكسسوارات تركية وصينية", "FG-0119", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "سلة توابل 15سم صيني درجة اولي", null, 0m, 500m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000120"), "إكسسوارات تركية وصينية", "FG-0120", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "سلة توابل 24 سم صيني درجة اولي", null, 0m, 550m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000121"), "إكسسوارات تركية وصينية", "FG-0121", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "سلة زاوية كروم صيني درجة اولي", null, 0m, 800m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000122"), "إكسسوارات تركية وصينية", "FG-0122", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "سلة زاوية فاصوليا صيني درجة اولي", null, 0m, 1100m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000123"), "إكسسوارات تركية وصينية", "FG-0123", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "سلة زاوية خبز صيني درجة اولي", null, 0m, 500m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000124"), "إكسسوارات تركية وصينية", "FG-0124", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "سقطار متحرك صيني درجة اولي- 90", null, 0m, 1100m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000125"), "إكسسوارات تركية وصينية", "FG-0125", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "سقطار متحرك صيني درجة اولي- 80", null, 0m, 1050m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000126"), "إكسسوارات تركية وصينية", "FG-0126", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "سقطار متحرك صيني درجة اولي- 70", null, 0m, 1000m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000127"), "إكسسوارات تركية وصينية", "FG-0127", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "سقطار ثابت صيني درجة اولي-90", null, 0m, 170m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000128"), "إكسسوارات تركية وصينية", "FG-0128", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "سقطار ثابت صيني درجة اولي-80", null, 0m, 170m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000129"), "إضاءة LED", "FG-0129", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "توريد وتركيب إضاءة (LED) صيني درجة اولي لفنرينة الزجاج", null, 0m, 170m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000130"), "إضاءة LED", "FG-0130", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "توريد وتركيب إضاءة (LED) صيني درجة اولي العلوي من اسفل", null, 0m, 170m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000131"), "إضاءة LED", "FG-0131", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "توريد وتركيب إضاءة (LED) صيني درجة اولي للسفلي من سكلف", null, 0m, 170m, 2, "متر طولي" },
                    { new Guid("f0000000-0000-4000-8000-000000000132"), "أجهزة وإكسسوارات ذكية", "FG-0132", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "اكسسوار متحرك عمودى (أوامر صوتيه) 90", null, 0m, 8950m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000133"), "أجهزة وإكسسوارات ذكية", "FG-0133", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("c4000000-0000-4000-8000-000000000004"), 0m, null, "شفاط خلف الغاز على السطح", null, 0m, 9750m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000134"), "الخدمات والتجهيزات", "FG-0134", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("d1000000-0000-4000-8000-000000000001"), 0m, null, "نقل ومناولة", null, 0m, 400m, 2, "خدمة" },
                    { new Guid("f0000000-0000-4000-8000-000000000135"), "الخدمات والتجهيزات", "FG-0135", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("d1000000-0000-4000-8000-000000000001"), 0m, null, "طاوله نشاره", "1م*", 0m, 2250m, 2, "قطعة" },
                    { new Guid("f0000000-0000-4000-8000-000000000136"), "الخدمات والتجهيزات", "FG-0136", new DateTime(2026, 9, 5, 13, 1, 21, 0, DateTimeKind.Utc), true, new Guid("d1000000-0000-4000-8000-000000000001"), 0m, null, "تركيب معدات", null, 0m, 400m, 2, "خدمة" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Code",
                table: "Accounts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ParentId",
                table: "Accounts",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_SystemKey",
                table: "Accounts",
                column: "SystemKey",
                unique: true,
                filter: "[SystemKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_ProjectId",
                table: "Contracts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_QuoteId",
                table: "Contracts",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_ContractId",
                table: "CustomerPayments",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerPayments_ProjectId",
                table: "CustomerPayments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ProjectId",
                table: "Expenses",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemGroups_Code",
                table: "ItemGroups",
                column: "Code",
                unique: true);

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
                name: "IX_JournalEntries_EntryDate",
                table: "JournalEntries",
                column: "EntryDate");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_EntryNo",
                table: "JournalEntries",
                column: "EntryNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_SourceType_SourceId",
                table: "JournalEntries",
                columns: new[] { "SourceType", "SourceId" },
                unique: true,
                filter: "[SourceType] IS NOT NULL AND [SourceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_AccountId",
                table: "JournalLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_JournalEntryId",
                table: "JournalLines",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequestItems_ItemId",
                table: "MaterialRequestItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequestItems_MaterialRequestId",
                table: "MaterialRequestItems",
                column: "MaterialRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequests_DecidedById",
                table: "MaterialRequests",
                column: "DecidedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequests_ProjectId",
                table: "MaterialRequests",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequests_PurchaseId",
                table: "MaterialRequests",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequests_RequestedById",
                table: "MaterialRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequests_RequestNo",
                table: "MaterialRequests",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialRequests_WarehouseId",
                table: "MaterialRequests",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CustomerId",
                table: "Projects",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_DesignerUserId",
                table: "Projects",
                column: "DesignerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_EngineerUserId",
                table: "Projects",
                column: "EngineerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_SalesUserId",
                table: "Projects",
                column: "SalesUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_ItemId",
                table: "PurchaseItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_PurchaseId",
                table: "PurchaseItems",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnItems_ItemId",
                table: "PurchaseReturnItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnItems_PurchaseReturnId",
                table: "PurchaseReturnItems",
                column: "PurchaseReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_PurchaseId",
                table: "PurchaseReturns",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_ReturnNo",
                table: "PurchaseReturns",
                column: "ReturnNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_SupplierId",
                table: "PurchaseReturns",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_WarehouseId",
                table: "PurchaseReturns",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_ProjectId",
                table: "Purchases",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_SupplierId",
                table: "Purchases",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_WarehouseId",
                table: "Purchases",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_ItemId",
                table: "QuoteItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_QuoteId",
                table: "QuoteItems",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_ProjectId",
                table: "Quotes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_EngineerId",
                table: "SiteVisits",
                column: "EngineerId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteVisits_ProjectId",
                table: "SiteVisits",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_ItemId",
                table: "StockTransactions",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_ProjectId",
                table: "StockTransactions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_PurchaseId",
                table: "StockTransactions",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_PurchaseReturnId",
                table: "StockTransactions",
                column: "PurchaseReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_WarehouseId",
                table: "StockTransactions",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_PurchaseId",
                table: "SupplierPayments",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_SupplierId",
                table: "SupplierPayments",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Contact");

            migrationBuilder.DropTable(
                name: "CustomerPayments");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "JournalLines");

            migrationBuilder.DropTable(
                name: "MaterialRequestItems");

            migrationBuilder.DropTable(
                name: "PeriodCloses");

            migrationBuilder.DropTable(
                name: "PurchaseItems");

            migrationBuilder.DropTable(
                name: "PurchaseReturnItems");

            migrationBuilder.DropTable(
                name: "QuoteItems");

            migrationBuilder.DropTable(
                name: "SiteInfo");

            migrationBuilder.DropTable(
                name: "SiteState");

            migrationBuilder.DropTable(
                name: "SiteVisits");

            migrationBuilder.DropTable(
                name: "StockTransactions");

            migrationBuilder.DropTable(
                name: "SupplierPayments");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropTable(
                name: "MaterialRequests");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "PurchaseReturns");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "ItemGroups");

            migrationBuilder.DropTable(
                name: "Purchases");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Warehouses");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
