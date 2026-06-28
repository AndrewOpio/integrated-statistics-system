using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RtgsVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RtgsVerified",
                table: "WithdrawalRequests");

            migrationBuilder.AddColumn<string>(
                name: "RtgsVerificationStatus",
                table: "WithdrawalRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "RtgsVerifiedDate",
                table: "WithdrawalRequests",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RtgsVerificationStatus",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "RtgsVerifiedDate",
                table: "WithdrawalRequests");

            migrationBuilder.AddColumn<bool>(
                name: "RtgsVerified",
                table: "WithdrawalRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
