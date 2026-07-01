using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameMonthlySurplusIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MonthlySettlementId",
                table: "MonthlySettlementStatusHistories",
                newName: "MonthlySurplusId");

            migrationBuilder.RenameIndex(
                name: "IX_MonthlySettlementStatusHistories_MonthlySettlementId",
                table: "MonthlySettlementStatusHistories",
                newName: "IX_MonthlySettlementStatusHistories_MonthlySurplusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MonthlySurplusId",
                table: "MonthlySettlementStatusHistories",
                newName: "MonthlySettlementId");

            migrationBuilder.RenameIndex(
                name: "IX_MonthlySettlementStatusHistories_MonthlySurplusId",
                table: "MonthlySettlementStatusHistories",
                newName: "IX_MonthlySettlementStatusHistories_MonthlySettlementId");
        }
    }
}
