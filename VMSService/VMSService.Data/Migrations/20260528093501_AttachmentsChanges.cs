using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AttachmentsChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssistantSupervisorUserId",
                table: "ProcessingBoards");

            migrationBuilder.DropColumn(
                name: "BoardSupervisorUserId",
                table: "ProcessingBoards");

            migrationBuilder.RenameColumn(
                name: "Attachments",
                table: "DepositRequestStatusHistory",
                newName: "AttachmentIds");

            migrationBuilder.RenameColumn(
                name: "ContentType",
                table: "Attachments",
                newName: "FileType");

            migrationBuilder.AlterColumn<Guid>(
                name: "DepositRequestDenominationId",
                table: "StockProcessingSessions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "BagId",
                table: "StockProcessingSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityId",
                table: "Attachments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "Attachments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "Attachments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "TransitionAttachmentRequirementId",
                table: "Attachments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowTransitionId",
                table: "Attachments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Bags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepositRequestDenominationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bags_DepositRequestDenominations_DepositRequestDenominationId",
                        column: x => x.DepositRequestDenominationId,
                        principalTable: "DepositRequestDenominations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockProcessingSessions_BagId",
                table: "StockProcessingSessions",
                column: "BagId");

            migrationBuilder.CreateIndex(
                name: "IX_StockProcessingSessions_DepositRequestDenominationId",
                table: "StockProcessingSessions",
                column: "DepositRequestDenominationId");

            migrationBuilder.CreateIndex(
                name: "IX_Bags_DepositRequestDenominationId",
                table: "Bags",
                column: "DepositRequestDenominationId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockProcessingSessions_Bags_BagId",
                table: "StockProcessingSessions",
                column: "BagId",
                principalTable: "Bags",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockProcessingSessions_Bags_BagId",
                table: "StockProcessingSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_StockProcessingSessions_DepositRequestDenominations_DepositRequestDenominationId",
                table: "StockProcessingSessions");

            migrationBuilder.DropTable(
                name: "Bags");

            migrationBuilder.DropIndex(
                name: "IX_StockProcessingSessions_BagId",
                table: "StockProcessingSessions");

            migrationBuilder.DropIndex(
                name: "IX_StockProcessingSessions_DepositRequestDenominationId",
                table: "StockProcessingSessions");

            migrationBuilder.DropColumn(
                name: "BagId",
                table: "StockProcessingSessions");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "TransitionAttachmentRequirementId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "WorkflowTransitionId",
                table: "Attachments");

            migrationBuilder.RenameColumn(
                name: "AttachmentIds",
                table: "DepositRequestStatusHistory",
                newName: "Attachments");

            migrationBuilder.RenameColumn(
                name: "FileType",
                table: "Attachments",
                newName: "ContentType");

            migrationBuilder.AlterColumn<int>(
                name: "DepositRequestDenominationId",
                table: "StockProcessingSessions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssistantSupervisorUserId",
                table: "ProcessingBoards",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BoardSupervisorUserId",
                table: "ProcessingBoards",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
