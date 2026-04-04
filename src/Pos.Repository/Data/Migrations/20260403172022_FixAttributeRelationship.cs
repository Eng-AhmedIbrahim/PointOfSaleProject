using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pos.Repository.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixAttributeRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MenuSalesItems_AttributeId",
                table: "MenuSalesItems");

            migrationBuilder.CreateIndex(
                name: "IX_MenuSalesItems_AttributeId",
                table: "MenuSalesItems",
                column: "AttributeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MenuSalesItems_AttributeId",
                table: "MenuSalesItems");

            migrationBuilder.CreateIndex(
                name: "IX_MenuSalesItems_AttributeId",
                table: "MenuSalesItems",
                column: "AttributeId",
                unique: true,
                filter: "[AttributeId] IS NOT NULL");
        }
    }
}
