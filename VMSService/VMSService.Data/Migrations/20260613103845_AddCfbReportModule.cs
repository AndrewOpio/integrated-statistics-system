using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    public partial class AddCfbReportModule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CfbReports",
                columns: table => new
                {
                    Id                = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber   = table.Column<string>(nullable: false),
                    PeriodStart       = table.Column<DateTime>(nullable: false),
                    PeriodEnd         = table.Column<DateTime>(nullable: false),
                    BranchId          = table.Column<int>(nullable: false),
                    Remarks           = table.Column<string>(nullable: true),
                    CurrentStatusId   = table.Column<int>(nullable: false),
                    CurrentStatusName = table.Column<string>(nullable: false, defaultValue: "Draft"),
                    CreatedDate  = table.Column<DateTime>(nullable: false),
                    ModifiedDate = table.Column<DateTime>(nullable: false),
                    CreatedBy    = table.Column<string>(nullable: false),
                    ModifiedBy   = table.Column<string>(nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_CfbReports", x => x.Id));

            migrationBuilder.CreateTable(
                name: "CfbReportTransactions",
                columns: table => new
                {
                    Id                   = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CfbReportId          = table.Column<int>(nullable: true),
                    BranchId             = table.Column<int>(nullable: false),
                    TellerName           = table.Column<string>(nullable: false),
                    TransactionDate      = table.Column<DateTime>(nullable: false),
                    TransactionReference = table.Column<string>(nullable: true),
                    Type                 = table.Column<string>(nullable: false),
                    DenominationId       = table.Column<int>(nullable: false),
                    Amount               = table.Column<decimal>(nullable: false),
                    CreatedDate  = table.Column<DateTime>(nullable: false),
                    ModifiedDate = table.Column<DateTime>(nullable: false),
                    CreatedBy    = table.Column<string>(nullable: false),
                    ModifiedBy   = table.Column<string>(nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CfbReportTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CfbReportTransactions_CfbReports_CfbReportId",
                        column: x => x.CfbReportId,
                        principalTable: "CfbReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CfbReportStatusHistories",
                columns: table => new
                {
                    Id            = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    CfbReportId   = table.Column<int>(nullable: false),
                    OldStatusId   = table.Column<int>(nullable: false),
                    OldStatusName = table.Column<string>(nullable: false),
                    NewStatusId   = table.Column<int>(nullable: false),
                    NewStatusName = table.Column<string>(nullable: false),
                    ChangedBy     = table.Column<string>(nullable: false),
                    ChangedAt     = table.Column<DateTime>(nullable: false),
                    Remarks       = table.Column<string>(nullable: true),
                    IsDeleted     = table.Column<bool>(nullable: false),
                    CreatedDate  = table.Column<DateTime>(nullable: false),
                    ModifiedDate = table.Column<DateTime>(nullable: false),
                    CreatedBy    = table.Column<string>(nullable: false),
                    ModifiedBy   = table.Column<string>(nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CfbReportStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CfbReportStatusHistories_CfbReports_CfbReportId",
                        column: x => x.CfbReportId,
                        principalTable: "CfbReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CfbReportTransactions_CfbReportId",
                table: "CfbReportTransactions",
                column: "CfbReportId");

            migrationBuilder.CreateIndex(
                name: "IX_CfbReportStatusHistories_CfbReportId",
                table: "CfbReportStatusHistories",
                column: "CfbReportId");

            migrationBuilder.Sql(@"
                INSERT INTO CfbReportTransactions
                    (CfbReportId, BranchId, TellerName, TransactionDate, TransactionReference, Type, DenominationId, Amount, CreatedDate, ModifiedDate, CreatedBy, ModifiedBy)
                VALUES
                (NULL, 1, 'Alice Nakimera',  '2026-06-09', 'TXN-2026-0001', 'Shortage', 1, 50000,  GETDATE(), GETDATE(), 'seed', 'seed'),
                (NULL, 1, 'Alice Nakimera',  '2026-06-09', 'TXN-2026-0002', 'Shortage', 2, 20000,  GETDATE(), GETDATE(), 'seed', 'seed'),
                (NULL, 1, 'Brian Omondi',    '2026-06-09', 'TXN-2026-0003', 'Surplus',  1, 30000,  GETDATE(), GETDATE(), 'seed', 'seed'),
                (NULL, 1, 'Carol Apio',      '2026-06-09', 'TXN-2026-0004', 'Shortage', 3, 15000,  GETDATE(), GETDATE(), 'seed', 'seed'),
                (NULL, 1, 'David Mukasa',    '2026-06-10', 'TXN-2026-0005', 'Surplus',  2, 45000,  GETDATE(), GETDATE(), 'seed', 'seed'),
                (NULL, 1, 'Alice Nakimera',  '2026-06-10', 'TXN-2026-0006', 'Shortage', 1, 10000,  GETDATE(), GETDATE(), 'seed', 'seed'),
                (NULL, 1, 'Brian Omondi',    '2026-06-10', 'TXN-2026-0007', 'Shortage', 3, 25000,  GETDATE(), GETDATE(), 'seed', 'seed'),
                (NULL, 1, 'Carol Apio',      '2026-06-10', 'TXN-2026-0008', 'Surplus',  2, 80000,  GETDATE(), GETDATE(), 'seed', 'seed'),
                (NULL, 2, 'Eve Namuli',      '2026-06-09', 'TXN-2026-0009', 'Surplus',  1, 60000,  GETDATE(), GETDATE(), 'seed', 'seed'),
                (NULL, 2, 'Frank Ssebuliba', '2026-06-09', 'TXN-2026-0010', 'Shortage', 2, 35000,  GETDATE(), GETDATE(), 'seed', 'seed'),
                (NULL, 2, 'Grace Auma',      '2026-06-10', 'TXN-2026-0011', 'Surplus',  1, 20000,  GETDATE(), GETDATE(), 'seed', 'seed'),
                (NULL, 2, 'Frank Ssebuliba', '2026-06-10', 'TXN-2026-0012', 'Shortage', 3, 40000,  GETDATE(), GETDATE(), 'seed', 'seed');
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CfbReportTransactions");
            migrationBuilder.DropTable(name: "CfbReportStatusHistories");
            migrationBuilder.DropTable(name: "CfbReports");
        }
    }
}
