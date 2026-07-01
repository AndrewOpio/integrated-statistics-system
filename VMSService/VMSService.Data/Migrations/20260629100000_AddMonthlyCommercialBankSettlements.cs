using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VMSService.Data.Configurations;

#nullable disable

namespace VMSService.Data.Migrations
{
    [DbContext(typeof(VMSServiceDbContext))]
    [Migration("20260629100000_AddMonthlyCommercialBankSettlements")]
    public partial class AddMonthlyCommercialBankSettlements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonthlyCommercialBankSettlements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber   = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommercialBankId  = table.Column<int>(type: "int", nullable: false),
                    Period            = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalSurplus      = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalShortage     = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetPosition       = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Remarks           = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentStatusId   = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy         = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate       = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy        = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate      = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyCommercialBankSettlements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyCommercialBankSettlementStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthlyCommercialBankSettlementId = table.Column<int>(type: "int", nullable: false),
                    OldStatusId   = table.Column<int>(type: "int", nullable: false),
                    OldStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewStatusId   = table.Column<int>(type: "int", nullable: false),
                    NewStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedBy     = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt     = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remarks       = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted     = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy     = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate   = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy    = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate  = table.Column<DateTime>(type: "datetime2", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyCommercialBankSettlementStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyCommercialBankSettlementStatusHistories_MonthlyCommercialBankSettlements_MonthlyCommercialBankSettlementId",
                        column: x => x.MonthlyCommercialBankSettlementId,
                        principalTable: "MonthlyCommercialBankSettlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyCommercialBankSettlements_CommercialBankId_Period",
                table: "MonthlyCommercialBankSettlements",
                columns: new[] { "CommercialBankId", "Period" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyCommercialBankSettlementStatusHistories_MonthlyCommercialBankSettlementId",
                table: "MonthlyCommercialBankSettlementStatusHistories",
                column: "MonthlyCommercialBankSettlementId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MonthlyCommercialBankSettlementStatusHistories");
            migrationBuilder.DropTable(name: "MonthlyCommercialBankSettlements");
        }
    }
}
