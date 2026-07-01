using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VMSService.Data.Configurations;

#nullable disable

namespace VMSService.Data.Migrations
{
    [DbContext(typeof(VMSServiceDbContext))]
    [Migration("20260626100001_RenameMonthlySettlementsToMonthlySurpluses")]
    public partial class RenameMonthlySettlementsToMonthlySurpluses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "MonthlySettlements",
                newName: "MonthlySurpluses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "MonthlySurpluses",
                newName: "MonthlySettlements");
        }
    }
}
