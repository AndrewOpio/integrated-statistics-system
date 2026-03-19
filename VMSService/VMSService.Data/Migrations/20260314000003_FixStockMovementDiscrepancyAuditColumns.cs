using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixStockMovementDiscrepancyAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old columns
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "StockMovementDiscrepancies");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "StockMovementDiscrepancies");

            // Add the correct columns
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "StockMovementDiscrepancies",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "StockMovementDiscrepancies",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "StockMovementDiscrepancies");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "StockMovementDiscrepancies");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "StockMovementDiscrepancies",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2026, 3, 14, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "StockMovementDiscrepancies",
                type: "datetime2",
                nullable: true);
        }
    }
}
