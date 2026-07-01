using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using VMSService.Data.Configurations;

#nullable disable

namespace VMSService.Data.Migrations
{
    [DbContext(typeof(VMSServiceDbContext))]
    [Migration("20260628100000_AddEodDiscrepancyReportEntries")]
    public partial class AddEodDiscrepancyReportEntries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EodDiscrepancyReportEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EodDiscrepancyReportId = table.Column<int>(type: "int", nullable: false),
                    BoardSessionSurplusShortageId = table.Column<int>(type: "int", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EodDiscrepancyReportEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EodDiscrepancyReportEntries_EodDiscrepancyReports_EodDiscrepancyReportId",
                        column: x => x.EodDiscrepancyReportId,
                        principalTable: "EodDiscrepancyReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EodDiscrepancyReportEntries_BoardSessionSurplusShortages_BoardSessionSurplusShortageId",
                        column: x => x.BoardSessionSurplusShortageId,
                        principalTable: "BoardSessionSurplusShortages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EodDiscrepancyReportEntries_EodDiscrepancyReportId",
                table: "EodDiscrepancyReportEntries",
                column: "EodDiscrepancyReportId");

            migrationBuilder.CreateIndex(
                name: "IX_EodDiscrepancyReportEntries_BoardSessionSurplusShortageId",
                table: "EodDiscrepancyReportEntries",
                column: "BoardSessionSurplusShortageId",
                unique: true);

            migrationBuilder.DropForeignKey(
                name: "FK_BoardSessionSurplusShortages_CfbReports",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropIndex(
                name: "IX_BoardSessionSurplusShortages_EodDiscrepancyReportId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropColumn(
                name: "EodDiscrepancyReportId",
                table: "BoardSessionSurplusShortages");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EodDiscrepancyReportId",
                table: "BoardSessionSurplusShortages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardSessionSurplusShortages_EodDiscrepancyReportId",
                table: "BoardSessionSurplusShortages",
                column: "EodDiscrepancyReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardSessionSurplusShortages_CfbReports",
                table: "BoardSessionSurplusShortages",
                column: "EodDiscrepancyReportId",
                principalTable: "EodDiscrepancyReports",
                principalColumn: "Id");

            migrationBuilder.DropTable(name: "EodDiscrepancyReportEntries");
        }
    }
}
