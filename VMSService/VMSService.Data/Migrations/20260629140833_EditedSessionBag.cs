using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class EditedSessionBag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionBags_StockProcessingSessions_StockProcessingSessionId",
                table: "SessionBags");

            migrationBuilder.RenameColumn(
                name: "StockProcessingSessionId",
                table: "SessionBags",
                newName: "StockProcessingPlanId");

            migrationBuilder.RenameIndex(
                name: "IX_SessionBags_StockProcessingSessionId",
                table: "SessionBags",
                newName: "IX_SessionBags_StockProcessingPlanId");

            migrationBuilder.AddColumn<int>(
                name: "ProcessingBoardId",
                table: "SessionBags",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SessionBags_ProcessingBoardId",
                table: "SessionBags",
                column: "ProcessingBoardId");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionBags_ProcessingBoards_ProcessingBoardId",
                table: "SessionBags",
                column: "ProcessingBoardId",
                principalTable: "ProcessingBoards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SessionBags_StockProcessingPlans_StockProcessingPlanId",
                table: "SessionBags",
                column: "StockProcessingPlanId",
                principalTable: "StockProcessingPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SessionBags_ProcessingBoards_ProcessingBoardId",
                table: "SessionBags");

            migrationBuilder.DropForeignKey(
                name: "FK_SessionBags_StockProcessingPlans_StockProcessingPlanId",
                table: "SessionBags");

            migrationBuilder.DropIndex(
                name: "IX_SessionBags_ProcessingBoardId",
                table: "SessionBags");

            migrationBuilder.DropColumn(
                name: "ProcessingBoardId",
                table: "SessionBags");

            migrationBuilder.RenameColumn(
                name: "StockProcessingPlanId",
                table: "SessionBags",
                newName: "StockProcessingSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_SessionBags_StockProcessingPlanId",
                table: "SessionBags",
                newName: "IX_SessionBags_StockProcessingSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SessionBags_StockProcessingSessions_StockProcessingSessionId",
                table: "SessionBags",
                column: "StockProcessingSessionId",
                principalTable: "StockProcessingSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
