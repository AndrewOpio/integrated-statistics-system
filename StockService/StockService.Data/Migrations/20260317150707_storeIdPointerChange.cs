using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockService.Data.Migrations
{
    public partial class storeIdPointerChange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockLedger_BranchCategories_StoreId",
                table: "StockLedger");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLedgerDetails_Categories_CategoryId",
                table: "StockLedgerDetails");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "StockLedgerDetails",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedger_Branches_StoreId",
                table: "StockLedger",
                column: "StoreId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedgerDetails_Categories_CategoryId",
                table: "StockLedgerDetails",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockLedger_Branches_StoreId",
                table: "StockLedger");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLedgerDetails_Categories_CategoryId",
                table: "StockLedgerDetails");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "StockLedgerDetails",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedger_BranchCategories_StoreId",
                table: "StockLedger",
                column: "StoreId",
                principalTable: "BranchCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedgerDetails_Categories_CategoryId",
                table: "StockLedgerDetails",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }
    }
}
