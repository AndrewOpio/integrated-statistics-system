using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class updatedwithdrawals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CbBranchId",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "SupervisorUserId",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "cbUserId",
                table: "WithdrawalRequests");

            migrationBuilder.RenameColumn(
                name: "InitiatorUserId",
                table: "WithdrawalRequests",
                newName: "BouBranchCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BouBranchCode",
                table: "WithdrawalRequests",
                newName: "InitiatorUserId");

            migrationBuilder.AddColumn<int>(
                name: "CbBranchId",
                table: "WithdrawalRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SupervisorUserId",
                table: "WithdrawalRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cbUserId",
                table: "WithdrawalRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
