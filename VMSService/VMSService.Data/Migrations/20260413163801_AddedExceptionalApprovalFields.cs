using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedExceptionalApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropColumn(
            //    name: "SpecialApprovalAt",
            //    table: "WithdrawalRequests");

            //migrationBuilder.DropColumn(
            //    name: "SpecialApprovalBy",
            //    table: "WithdrawalRequests");

            //migrationBuilder.AddColumn<bool>(
            //    name: "IsDeleted",
            //    table: "WithdrawalRequests",
            //    type: "bit",
            //    nullable: false,
            //    defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DepositRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresSpecialApproval",
                table: "DepositRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SpecialApprovalReason",
                table: "DepositRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropColumn(
            //    name: "IsDeleted",
            //    table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DepositRequests");

            migrationBuilder.DropColumn(
                name: "RequiresSpecialApproval",
                table: "DepositRequests");

            migrationBuilder.DropColumn(
                name: "SpecialApprovalReason",
                table: "DepositRequests");

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "SpecialApprovalAt",
            //    table: "WithdrawalRequests",
            //    type: "datetime2",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "SpecialApprovalBy",
            //    table: "WithdrawalRequests",
            //    type: "nvarchar(max)",
            //    nullable: true);
        }
    }
}
