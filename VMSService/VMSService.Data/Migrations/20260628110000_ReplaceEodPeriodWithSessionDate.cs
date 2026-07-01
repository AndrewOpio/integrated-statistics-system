using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using VMSService.Data.Configurations;

#nullable disable

namespace VMSService.Data.Migrations
{
    [DbContext(typeof(VMSServiceDbContext))]
    [Migration("20260628110000_ReplaceEodPeriodWithSessionDate")]
    public partial class ReplaceEodPeriodWithSessionDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SessionDate",
                table: "EodDiscrepancyReports",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql("UPDATE [EodDiscrepancyReports] SET [SessionDate] = [PeriodStart]");

            migrationBuilder.DropColumn(
                name: "PeriodEnd",
                table: "EodDiscrepancyReports");

            migrationBuilder.DropColumn(
                name: "PeriodStart",
                table: "EodDiscrepancyReports");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PeriodStart",
                table: "EodDiscrepancyReports",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "PeriodEnd",
                table: "EodDiscrepancyReports",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql("UPDATE [EodDiscrepancyReports] SET [PeriodStart] = [SessionDate], [PeriodEnd] = [SessionDate]");

            migrationBuilder.DropColumn(
                name: "SessionDate",
                table: "EodDiscrepancyReports");
        }
    }
}
