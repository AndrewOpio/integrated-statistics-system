using Microsoft.EntityFrameworkCore.Migrations;

namespace VMSService.Data.Migrations
{
    public partial class AddLogisticsIdToStockMovement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LogisticsId",
                table: "StockMovements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_LogisticsId",
                table: "StockMovements",
                column: "LogisticsId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Logistics_LogisticsId",
                table: "StockMovements",
                column: "LogisticsId",
                principalTable: "Logistics",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Logistics_LogisticsId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_LogisticsId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "LogisticsId",
                table: "StockMovements");
        }
    }
}
