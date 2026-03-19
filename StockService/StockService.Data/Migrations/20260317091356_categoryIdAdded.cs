using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockService.Data.Migrations
{
    public partial class categoryIdAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First add the column as nullable
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "StockLedgerDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockLedgerDetails_CategoryId",
                table: "StockLedgerDetails",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedgerDetails_Categories_CategoryId",
                table: "StockLedgerDetails",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockLedgerDetails_Categories_CategoryId",
                table: "StockLedgerDetails");

            migrationBuilder.DropIndex(
                name: "IX_StockLedgerDetails_CategoryId",
                table: "StockLedgerDetails");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "StockLedgerDetails");
        }
    }
}
