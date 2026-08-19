using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovedFitUnfitColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FitValue",
                table: "StockProcessingResults");

            migrationBuilder.DropColumn(
                name: "UnfitShredValue",
                table: "StockProcessingResults");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FitValue",
                table: "StockProcessingResults",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnfitShredValue",
                table: "StockProcessingResults",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
