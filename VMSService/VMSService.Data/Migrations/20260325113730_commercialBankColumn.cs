using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class commercialBankColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Categorization",
                table: "StockMovements");

            migrationBuilder.AddColumn<int>(
                name: "CommercialBankId",
                table: "StockMovements",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommercialBankId",
                table: "StockMovements");

            migrationBuilder.AddColumn<string>(
                name: "Categorization",
                table: "StockMovements",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
