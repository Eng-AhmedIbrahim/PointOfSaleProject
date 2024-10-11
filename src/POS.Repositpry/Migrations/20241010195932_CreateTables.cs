using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Repository.Migrations
{
    /// <inheritdoc />
    public partial class CreateTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnglishName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    NormalizedEnglishName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    ArabicName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    MobileNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LogoWidth = table.Column<int>(type: "int", nullable: false, defaultValue: 200),
                    LogoHeight = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Phone1 = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Phone2 = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Suspend = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Branches_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArabicName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    NormalizedEnglishName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    ItemsFont = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: true),
                    Invisible = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    BranchId = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreationDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    UpdateDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuSalesItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Barcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArabicName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    EnglishName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    NormalizedEnglishName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SecondPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ThirdPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FourthPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FifthPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MainCategoryId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BackColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    TextColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    TextSize = table.Column<int>(type: "int", nullable: true),
                    Invisible = table.Column<bool>(type: "bit", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BranchId = table.Column<int>(type: "int", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    HasAttribute = table.Column<bool>(type: "bit", nullable: false),
                    AttributeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuSalesItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuSalesItems_Attributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "Attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MenuSalesItems_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MenuSalesItems_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttributeItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppearanceIndex = table.Column<int>(type: "int", nullable: false),
                    AttributeId = table.Column<int>(type: "int", nullable: false),
                    RelatedMenuItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttributeItems_Attributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "Attributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttributeItems_MenuSalesItems_RelatedMenuItemId",
                        column: x => x.RelatedMenuItemId,
                        principalTable: "MenuSalesItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeItems_AttributeId",
                table: "AttributeItems",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeItems_RelatedMenuItemId",
                table: "AttributeItems",
                column: "RelatedMenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_CompanyId",
                table: "Branches",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_BranchId",
                table: "Categories",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuSalesItems_AttributeId",
                table: "MenuSalesItems",
                column: "AttributeId",
                unique: true,
                filter: "[AttributeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MenuSalesItems_BranchId",
                table: "MenuSalesItems",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuSalesItems_CategoryId",
                table: "MenuSalesItems",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttributeItems");

            migrationBuilder.DropTable(
                name: "MenuSalesItems");

            migrationBuilder.DropTable(
                name: "Attributes");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
