using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class EditedResultsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockProcessingPlans_StockMovements_StockMovementId",
                table: "StockProcessingPlans");

            migrationBuilder.DropIndex(
                name: "IX_StockProcessingPlans_StockMovementId",
                table: "StockProcessingPlans");

            migrationBuilder.DropColumn(
                name: "StockMovementId",
                table: "StockProcessingPlans");

            migrationBuilder.AddColumn<decimal>(
                name: "FitValue",
                table: "StockProcessingResults",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnFitShredValue",
                table: "StockProcessingResults",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FitValue",
                table: "StockProcessingResults");

            migrationBuilder.DropColumn(
                name: "UnFitShredValue",
                table: "StockProcessingResults");

            migrationBuilder.AddColumn<int>(
                name: "StockMovementId",
                table: "StockProcessingPlans",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockProcessingPlans_StockMovementId",
                table: "StockProcessingPlans",
                column: "StockMovementId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockProcessingPlans_StockMovements_StockMovementId",
                table: "StockProcessingPlans",
                column: "StockMovementId",
                principalTable: "StockMovements",
                principalColumn: "Id");
        }
    }
}
