using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    public partial class cfbAddDenomValue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DenomValue",
                table: "CfbCashMovements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DenomValue",
                table: "CfbCashHoldings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DenomValue",
                table: "CfbCashMovements");

            migrationBuilder.DropColumn(
                name: "DenomValue",
                table: "CfbCashHoldings");
        }
    }
}