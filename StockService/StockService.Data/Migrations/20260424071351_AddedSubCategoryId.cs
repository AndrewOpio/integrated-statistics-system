using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockService.Data.Migrations
{
    public partial class AddedSubCategoryId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubCategoryId",
                table: "StockLedgerDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerDetails_SubCategoryId",
                table: "StockLedgerDetails",
                column: "SubCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedgerDetails_SubCategories_SubCategoryId",
                table: "StockLedgerDetails",
                column: "SubCategoryId",
                principalTable: "SubCategories",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockLedgerDetails_SubCategories_SubCategoryId",
                table: "StockLedgerDetails");

            migrationBuilder.DropIndex(
                name: "IX_StockLedgerDetails_SubCategoryId",
                table: "StockLedgerDetails");

            migrationBuilder.DropColumn(
                name: "SubCategoryId",
                table: "StockLedgerDetails");
        }
    }
}
