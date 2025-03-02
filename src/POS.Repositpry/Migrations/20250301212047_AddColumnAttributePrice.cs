using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnAttributePrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttributeItems_MenuSalesItems_RelatedMenuItemId",
                table: "AttributeItems");

            migrationBuilder.AddColumn<decimal>(
                name: "AttributePrice",
                table: "MenuSalesItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttributeItems_MenuSalesItems_RelatedMenuItemId",
                table: "AttributeItems",
                column: "RelatedMenuItemId",
                principalTable: "MenuSalesItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttributeItems_MenuSalesItems_RelatedMenuItemId",
                table: "AttributeItems");

            migrationBuilder.DropColumn(
                name: "AttributePrice",
                table: "MenuSalesItems");

            migrationBuilder.AddForeignKey(
                name: "FK_AttributeItems_MenuSalesItems_RelatedMenuItemId",
                table: "AttributeItems",
                column: "RelatedMenuItemId",
                principalTable: "MenuSalesItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
