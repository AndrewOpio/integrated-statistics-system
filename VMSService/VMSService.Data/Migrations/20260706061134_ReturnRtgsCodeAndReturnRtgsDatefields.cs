using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReturnRtgsCodeAndReturnRtgsDatefields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReturnRtgsCode",
                table: "WithdrawalRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ReturnRtgsDate",
                table: "WithdrawalRequests",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnRtgsCode",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "ReturnRtgsDate",
                table: "WithdrawalRequests");
        }
    }
}
