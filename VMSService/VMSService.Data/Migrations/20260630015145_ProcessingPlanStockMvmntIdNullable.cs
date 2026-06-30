using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProcessingPlanStockMvmntIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockProcessingPlans_StockMovements_StockMovementId",
                table: "StockProcessingPlans");

            migrationBuilder.AlterColumn<int>(
                name: "StockMovementId",
                table: "StockProcessingPlans",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_StockProcessingPlans_StockMovements_StockMovementId",
                table: "StockProcessingPlans",
                column: "StockMovementId",
                principalTable: "StockMovements",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockProcessingPlans_StockMovements_StockMovementId",
                table: "StockProcessingPlans");

            migrationBuilder.AlterColumn<int>(
                name: "StockMovementId",
                table: "StockProcessingPlans",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StockProcessingPlans_StockMovements_StockMovementId",
                table: "StockProcessingPlans",
                column: "StockMovementId",
                principalTable: "StockMovements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
