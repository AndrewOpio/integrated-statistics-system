using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProcessingAndShortageChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardSessionDifferentDenoms_BoardSessionSurplusShortages_BoardSessionSurplusShortageId",
                table: "BoardSessionDifferentDenoms");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardSessionSurplusShortages_DepositRequests_DepositRequestId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "StockProcessingSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalIssuedValue",
                table: "StockProcessingSessions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StockProcessingSessionId",
                table: "BoardSessionSurplusShortages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "BoardSessionSurplusShortageId",
                table: "BoardSessionDifferentDenoms",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "StockProcessingSessionId",
                table: "BoardSessionDifferentDenoms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BoardSessionSurplusShortages_StockProcessingSessionId",
                table: "BoardSessionSurplusShortages",
                column: "StockProcessingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSessionDifferentDenoms_StockProcessingSessionId",
                table: "BoardSessionDifferentDenoms",
                column: "StockProcessingSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardSessionDifferentDenoms_BoardSessionSurplusShortages_BoardSessionSurplusShortageId",
                table: "BoardSessionDifferentDenoms",
                column: "BoardSessionSurplusShortageId",
                principalTable: "BoardSessionSurplusShortages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardSessionDifferentDenoms_StockProcessingSessions_StockProcessingSessionId",
                table: "BoardSessionDifferentDenoms",
                column: "StockProcessingSessionId",
                principalTable: "StockProcessingSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardSessionSurplusShortages_DepositRequests_DepositRequestId",
                table: "BoardSessionSurplusShortages",
                column: "DepositRequestId",
                principalTable: "DepositRequests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardSessionSurplusShortages_StockProcessingSessions_StockProcessingSessionId",
                table: "BoardSessionSurplusShortages",
                column: "StockProcessingSessionId",
                principalTable: "StockProcessingSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardSessionDifferentDenoms_BoardSessionSurplusShortages_BoardSessionSurplusShortageId",
                table: "BoardSessionDifferentDenoms");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardSessionDifferentDenoms_StockProcessingSessions_StockProcessingSessionId",
                table: "BoardSessionDifferentDenoms");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardSessionSurplusShortages_DepositRequests_DepositRequestId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardSessionSurplusShortages_StockProcessingSessions_StockProcessingSessionId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropIndex(
                name: "IX_BoardSessionSurplusShortages_StockProcessingSessionId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropIndex(
                name: "IX_BoardSessionDifferentDenoms_StockProcessingSessionId",
                table: "BoardSessionDifferentDenoms");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "StockProcessingSessions");

            migrationBuilder.DropColumn(
                name: "TotalIssuedValue",
                table: "StockProcessingSessions");

            migrationBuilder.DropColumn(
                name: "StockProcessingSessionId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropColumn(
                name: "StockProcessingSessionId",
                table: "BoardSessionDifferentDenoms");

            migrationBuilder.AlterColumn<int>(
                name: "BoardSessionSurplusShortageId",
                table: "BoardSessionDifferentDenoms",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardSessionDifferentDenoms_BoardSessionSurplusShortages_BoardSessionSurplusShortageId",
                table: "BoardSessionDifferentDenoms",
                column: "BoardSessionSurplusShortageId",
                principalTable: "BoardSessionSurplusShortages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardSessionSurplusShortages_DepositRequests_DepositRequestId",
                table: "BoardSessionSurplusShortages",
                column: "DepositRequestId",
                principalTable: "DepositRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
