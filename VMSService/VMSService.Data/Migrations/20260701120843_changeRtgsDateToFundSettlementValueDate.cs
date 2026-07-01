using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class changeRtgsDateToFundSettlementValueDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RtgsDate",
                table: "WithdrawalRequests",
                newName: "FundSettlementValueDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FundSettlementValueDate",
                table: "WithdrawalRequests",
                newName: "RtgsDate");
        }
    }
}
