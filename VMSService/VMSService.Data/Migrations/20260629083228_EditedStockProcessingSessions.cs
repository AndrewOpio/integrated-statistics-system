using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class EditedStockProcessingSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockProcessingSessions_Bags_BagId",
                table: "StockProcessingSessions");

            migrationBuilder.DropIndex(
                name: "IX_StockProcessingSessions_BagId",
                table: "StockProcessingSessions");

            migrationBuilder.DropColumn(
                name: "BagId",
                table: "StockProcessingSessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BagId",
                table: "StockProcessingSessions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockProcessingSessions_BagId",
                table: "StockProcessingSessions",
                column: "BagId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockProcessingSessions_Bags_BagId",
                table: "StockProcessingSessions",
                column: "BagId",
                principalTable: "Bags",
                principalColumn: "Id");
        }
    }
}
