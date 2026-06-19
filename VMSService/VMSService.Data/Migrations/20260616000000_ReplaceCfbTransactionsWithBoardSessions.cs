using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    public partial class ReplaceCfbTransactionsWithBoardSessions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CfbReportTransactions");

            migrationBuilder.CreateTable(
                name: "BoardSessionSurplusShortages",
                columns: table => new
                {
                    Id                      = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CfbReportId             = table.Column<int>(nullable: true),
                    CommercialBankId        = table.Column<int>(nullable: false),
                    BranchId                = table.Column<int>(nullable: false),
                    SessionSourceBranch     = table.Column<int>(nullable: true),
                    DenominationId          = table.Column<int>(nullable: false),
                    Amount                  = table.Column<decimal>(nullable: false),
                    IsSurplus               = table.Column<bool>(nullable: false),
                    IsDifferentDenomination = table.Column<bool>(nullable: false),
                    IsProcessedOnRtgs       = table.Column<bool>(nullable: false),
                    RtgsCode                = table.Column<string>(nullable: true),
                    CreatedDate             = table.Column<DateTime>(nullable: false),
                    ModifiedDate            = table.Column<DateTime>(nullable: false),
                    CreatedBy               = table.Column<string>(nullable: false),
                    ModifiedBy              = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardSessionSurplusShortages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardSessionSurplusShortages_CfbReports_CfbReportId",
                        column: x => x.CfbReportId,
                        principalTable: "CfbReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BoardSessionDifferentDenoms",
                columns: table => new
                {
                    Id                              = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    BoardSessionSurplusShortageId   = table.Column<int>(nullable: false),
                    DenominationId                  = table.Column<int>(nullable: false),
                    Amount                          = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardSessionDifferentDenoms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardSessionDifferentDenoms_BoardSessionSurplusShortages_BoardSessionSurplusShortageId",
                        column: x => x.BoardSessionSurplusShortageId,
                        principalTable: "BoardSessionSurplusShortages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardSessionSurplusShortages_CfbReportId",
                table: "BoardSessionSurplusShortages",
                column: "CfbReportId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSessionDifferentDenoms_BoardSessionSurplusShortageId",
                table: "BoardSessionDifferentDenoms",
                column: "BoardSessionSurplusShortageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BoardSessionDifferentDenoms");
            migrationBuilder.DropTable(name: "BoardSessionSurplusShortages");
        }
    }
}
