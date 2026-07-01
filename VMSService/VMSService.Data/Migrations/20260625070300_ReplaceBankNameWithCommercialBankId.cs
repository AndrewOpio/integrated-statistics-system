using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceBankNameWithCommercialBankId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankName",
                table: "MonthlySettlements");

            migrationBuilder.AddColumn<int>(
                name: "CommercialBankId",
                table: "MonthlySettlements",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommercialBankId",
                table: "MonthlySettlements");

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "MonthlySettlements",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
