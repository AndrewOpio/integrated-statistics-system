using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkBoardSessionToDepositRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepositRequestId",
                table: "BoardSessionSurplusShortages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardSessionSurplusShortages_DepositRequestId",
                table: "BoardSessionSurplusShortages",
                column: "DepositRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardSessionSurplusShortages_DepositRequests_DepositRequestId",
                table: "BoardSessionSurplusShortages",
                column: "DepositRequestId",
                principalTable: "DepositRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardSessionSurplusShortages_DepositRequests_DepositRequestId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropIndex(
                name: "IX_BoardSessionSurplusShortages_DepositRequestId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropColumn(
                name: "DepositRequestId",
                table: "BoardSessionSurplusShortages");
        }
    }
}
