using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockService.Data.Migrations
{
    public partial class dbNameChange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockLedgerDetail_Denominations_DenominationId",
                table: "StockLedgerDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLedgerDetail_StockLedger_StockLedgerId",
                table: "StockLedgerDetail");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StockLedgerDetail",
                table: "StockLedgerDetail");

            migrationBuilder.DropColumn(
                name: "IsOut",
                table: "StockLedger");

            migrationBuilder.RenameTable(
                name: "StockLedgerDetail",
                newName: "StockLedgerDetails");

            migrationBuilder.RenameIndex(
                name: "IX_StockLedgerDetail_StockLedgerId",
                table: "StockLedgerDetails",
                newName: "IX_StockLedgerDetails_StockLedgerId");

            migrationBuilder.RenameIndex(
                name: "IX_StockLedgerDetail_DenominationId",
                table: "StockLedgerDetails",
                newName: "IX_StockLedgerDetails_DenominationId");

            migrationBuilder.AddColumn<bool>(
                name: "IsIn",
                table: "StockLedger",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_StockLedgerDetails",
                table: "StockLedgerDetails",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedgerDetails_Denominations_DenominationId",
                table: "StockLedgerDetails",
                column: "DenominationId",
                principalTable: "Denominations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedgerDetails_StockLedger_StockLedgerId",
                table: "StockLedgerDetails",
                column: "StockLedgerId",
                principalTable: "StockLedger",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockLedgerDetails_Denominations_DenominationId",
                table: "StockLedgerDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLedgerDetails_StockLedger_StockLedgerId",
                table: "StockLedgerDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StockLedgerDetails",
                table: "StockLedgerDetails");

            migrationBuilder.DropColumn(
                name: "IsIn",
                table: "StockLedger");

            migrationBuilder.RenameTable(
                name: "StockLedgerDetails",
                newName: "StockLedgerDetail");

            migrationBuilder.RenameIndex(
                name: "IX_StockLedgerDetails_StockLedgerId",
                table: "StockLedgerDetail",
                newName: "IX_StockLedgerDetail_StockLedgerId");

            migrationBuilder.RenameIndex(
                name: "IX_StockLedgerDetails_DenominationId",
                table: "StockLedgerDetail",
                newName: "IX_StockLedgerDetail_DenominationId");

            migrationBuilder.AddColumn<string>(
                name: "IsOut",
                table: "StockLedger",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StockLedgerDetail",
                table: "StockLedgerDetail",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedgerDetail_Denominations_DenominationId",
                table: "StockLedgerDetail",
                column: "DenominationId",
                principalTable: "Denominations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedgerDetail_StockLedger_StockLedgerId",
                table: "StockLedgerDetail",
                column: "StockLedgerId",
                principalTable: "StockLedger",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
