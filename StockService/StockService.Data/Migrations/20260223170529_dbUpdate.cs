using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockService.Data.Migrations
{
    public partial class dbUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CounterPartyTypeId",
                table: "StockLedger",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StockLedger_CounterPartyTypeId",
                table: "StockLedger",
                column: "CounterPartyTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedger_CounterPartyTypes_CounterPartyTypeId",
                table: "StockLedger",
                column: "CounterPartyTypeId",
                principalTable: "CounterPartyTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockLedger_CounterPartyTypes_CounterPartyTypeId",
                table: "StockLedger");

            migrationBuilder.DropIndex(
                name: "IX_StockLedger_CounterPartyTypeId",
                table: "StockLedger");

            migrationBuilder.DropColumn(
                name: "CounterPartyTypeId",
                table: "StockLedger");
        }
    }
}
