using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConfigurationsService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedBranchCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CBCode",
                table: "CommercialBanks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BranchCode",
                table: "Branches",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CBCode",
                table: "CommercialBanks");

            migrationBuilder.DropColumn(
                name: "BranchCode",
                table: "Branches");
        }
    }
}
