using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using VMSService.Data.Configurations;

#nullable disable

namespace VMSService.Data.Migrations
{
    [DbContext(typeof(VMSServiceDbContext))]
    [Migration("20260627225916_RenameStatusHistoryTable")]
    public partial class RenameStatusHistoryTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "MonthlySettlementStatusHistories",
                newName: "MonthlySurplusStatusHistories");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "MonthlySurplusStatusHistories",
                newName: "MonthlySettlementStatusHistories");
        }
    }
}
