using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    public partial class RenameEodDiscrepancyTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "CfbReports",
                newName: "EodDiscrepancyReports");

            migrationBuilder.RenameTable(
                name: "CfbReportStatusHistories",
                newName: "EodDiscrepancyReportStatusHistories");

            migrationBuilder.RenameColumn(
                name: "CfbReportId",
                table: "EodDiscrepancyReportStatusHistories",
                newName: "EodDiscrepancyReportId");

            migrationBuilder.RenameColumn(
                name: "CfbReportId",
                table: "BoardSessionSurplusShortages",
                newName: "EodDiscrepancyReportId");

            migrationBuilder.RenameIndex(
                name: "IX_BoardSessionSurplusShortages_CfbReportId",
                table: "BoardSessionSurplusShortages",
                newName: "IX_BoardSessionSurplusShortages_EodDiscrepancyReportId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_BoardSessionSurplusShortages_EodDiscrepancyReportId",
                table: "BoardSessionSurplusShortages",
                newName: "IX_BoardSessionSurplusShortages_CfbReportId");

            migrationBuilder.RenameColumn(
                name: "EodDiscrepancyReportId",
                table: "BoardSessionSurplusShortages",
                newName: "CfbReportId");

            migrationBuilder.RenameColumn(
                name: "EodDiscrepancyReportId",
                table: "EodDiscrepancyReportStatusHistories",
                newName: "CfbReportId");

            migrationBuilder.RenameTable(
                name: "EodDiscrepancyReportStatusHistories",
                newName: "CfbReportStatusHistories");

            migrationBuilder.RenameTable(
                name: "EodDiscrepancyReports",
                newName: "CfbReports");
        }
    }
}
