using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class BagDenominationNumberOfBundlesOrBoxes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberOfBundlesOrBoxes",
                table: "Bags");

            migrationBuilder.AddColumn<int>(
                name: "NumberOfBundlesOrBoxes",
                table: "BagDenominations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberOfBundlesOrBoxes",
                table: "BagDenominations");

            migrationBuilder.AddColumn<int>(
                name: "NumberOfBundlesOrBoxes",
                table: "Bags",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
