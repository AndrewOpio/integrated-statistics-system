using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class logisticsUpdatev2 : Migration
    {
        /// <inheritdoc />
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
                principalColumn: "Id");
        }

        /// <inheritdoc />
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
