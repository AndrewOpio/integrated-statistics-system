using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockService.Data.Migrations
{
    public partial class AddedNumberOfBoxesAndBundles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockLedger_Categories_CategoryId",
                table: "StockLedger");

            migrationBuilder.RenameColumn(
                name: "Values",
                table: "StockLedgerDetails",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "Count",
                table: "StockLedgerDetails",
                newName: "NumberOfPieces");

            migrationBuilder.AddColumn<int>(
                name: "NumberOfBoxes",
                table: "StockLedgerDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfBundles",
                table: "StockLedgerDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "StockLedger",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "StockLedger",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StockLedger_StoreId",
                table: "StockLedger",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedger_BranchCategories_StoreId",
                table: "StockLedger",
                column: "StoreId",
                principalTable: "BranchCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedger_Categories_CategoryId",
                table: "StockLedger",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockLedger_BranchCategories_StoreId",
                table: "StockLedger");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLedger_Categories_CategoryId",
                table: "StockLedger");

            migrationBuilder.DropIndex(
                name: "IX_StockLedger_StoreId",
                table: "StockLedger");

            migrationBuilder.DropColumn(
                name: "NumberOfBoxes",
                table: "StockLedgerDetails");

            migrationBuilder.DropColumn(
                name: "NumberOfBundles",
                table: "StockLedgerDetails");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "StockLedger");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "StockLedgerDetails",
                newName: "Values");

            migrationBuilder.RenameColumn(
                name: "NumberOfPieces",
                table: "StockLedgerDetails",
                newName: "Count");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "StockLedger",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedger_Categories_CategoryId",
                table: "StockLedger",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
