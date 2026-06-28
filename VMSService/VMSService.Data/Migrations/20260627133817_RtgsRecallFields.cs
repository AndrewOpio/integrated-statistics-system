using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RtgsRecallFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RecallRtgsCode",
                table: "WithdrawalRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RecallRtgsDate",
                table: "WithdrawalRequests",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RecallRtgsPending",
                table: "WithdrawalRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecallRtgsCode",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "RecallRtgsDate",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "RecallRtgsPending",
                table: "WithdrawalRequests");
        }
    }
}
