using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class DroppedStatusTypesTableFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovedCurrencyProcurements_StatusTypes_CurrentStatusId",
                table: "ApprovedCurrencyProcurements");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovedCurrencyStatusHistory_StatusTypes_NewStatusId",
                table: "ApprovedCurrencyStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovedCurrencyStatusHistory_StatusTypes_OldStatusId",
                table: "ApprovedCurrencyStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ExPrintMints_StatusTypes_CurrentStatusId",
                table: "ExPrintMints");

            migrationBuilder.DropForeignKey(
                name: "FK_ExPrintMintStatusHistory_StatusTypes_NewStatusId",
                table: "ExPrintMintStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ExPrintMintStatusHistory_StatusTypes_OldStatusId",
                table: "ExPrintMintStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedStock_StatusTypes_CurrentStatusId",
                table: "ReceivedStock");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedStockStatusHistory_StatusTypes_NewStatusId",
                table: "ReceivedStockStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedStockStatusHistory_StatusTypes_OldStatusId",
                table: "ReceivedStockStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_StatusTypes_CurrentStatusId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovementStatusHistory_StatusTypes_NewStatusId",
                table: "StockMovementStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovementStatusHistory_StatusTypes_OldStatusId",
                table: "StockMovementStatusHistory");

            migrationBuilder.DropTable(
                name: "WorkflowTransitions");

            migrationBuilder.DropTable(
                name: "StatusTypes");

            migrationBuilder.DropIndex(
                name: "IX_StockMovementStatusHistory_NewStatusId",
                table: "StockMovementStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_StockMovementStatusHistory_OldStatusId",
                table: "StockMovementStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_CurrentStatusId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_ReceivedStockStatusHistory_NewStatusId",
                table: "ReceivedStockStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_ReceivedStockStatusHistory_OldStatusId",
                table: "ReceivedStockStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_ReceivedStock_CurrentStatusId",
                table: "ReceivedStock");

            migrationBuilder.DropIndex(
                name: "IX_ExPrintMintStatusHistory_NewStatusId",
                table: "ExPrintMintStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_ExPrintMintStatusHistory_OldStatusId",
                table: "ExPrintMintStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_ExPrintMints_CurrentStatusId",
                table: "ExPrintMints");

            migrationBuilder.DropIndex(
                name: "IX_ApprovedCurrencyStatusHistory_NewStatusId",
                table: "ApprovedCurrencyStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_ApprovedCurrencyStatusHistory_OldStatusId",
                table: "ApprovedCurrencyStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_ApprovedCurrencyProcurements_CurrentStatusId",
                table: "ApprovedCurrencyProcurements");

            migrationBuilder.AddColumn<string>(
                name: "NewStatusName",
                table: "StockMovementStatusHistory",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OldStatusName",
                table: "StockMovementStatusHistory",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrentStatusName",
                table: "StockMovements",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrentStatusName",
                table: "StockMovementDiscrepancies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NewStatusName",
                table: "ReceivedStockStatusHistory",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OldStatusName",
                table: "ReceivedStockStatusHistory",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrentStatusName",
                table: "ReceivedStock",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NewStatusName",
                table: "ExPrintMintStatusHistory",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OldStatusName",
                table: "ExPrintMintStatusHistory",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrentStatusName",
                table: "ExPrintMints",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NewStatusName",
                table: "ApprovedCurrencyStatusHistory",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OldStatusName",
                table: "ApprovedCurrencyStatusHistory",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CurrentStatusName",
                table: "ApprovedCurrencyProcurements",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewStatusName",
                table: "StockMovementStatusHistory");

            migrationBuilder.DropColumn(
                name: "OldStatusName",
                table: "StockMovementStatusHistory");

            migrationBuilder.DropColumn(
                name: "CurrentStatusName",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "CurrentStatusName",
                table: "StockMovementDiscrepancies");

            migrationBuilder.DropColumn(
                name: "NewStatusName",
                table: "ReceivedStockStatusHistory");

            migrationBuilder.DropColumn(
                name: "OldStatusName",
                table: "ReceivedStockStatusHistory");

            migrationBuilder.DropColumn(
                name: "CurrentStatusName",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "NewStatusName",
                table: "ExPrintMintStatusHistory");

            migrationBuilder.DropColumn(
                name: "OldStatusName",
                table: "ExPrintMintStatusHistory");

            migrationBuilder.DropColumn(
                name: "CurrentStatusName",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "NewStatusName",
                table: "ApprovedCurrencyStatusHistory");

            migrationBuilder.DropColumn(
                name: "OldStatusName",
                table: "ApprovedCurrencyStatusHistory");

            migrationBuilder.DropColumn(
                name: "CurrentStatusName",
                table: "ApprovedCurrencyProcurements");

            migrationBuilder.CreateTable(
                name: "StatusTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTransitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromStatusId = table.Column<int>(type: "int", nullable: false),
                    ToStatusId = table.Column<int>(type: "int", nullable: false),
                    ActionName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ButtonClass = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisplayLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequiredRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiresComments = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_StatusTypes_FromStatusId",
                        column: x => x.FromStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_StatusTypes_ToStatusId",
                        column: x => x.ToStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementStatusHistory_NewStatusId",
                table: "StockMovementStatusHistory",
                column: "NewStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementStatusHistory_OldStatusId",
                table: "StockMovementStatusHistory",
                column: "OldStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CurrentStatusId",
                table: "StockMovements",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementDiscrepancies_CurrentStatusId",
                table: "StockMovementDiscrepancies",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockStatusHistory_NewStatusId",
                table: "ReceivedStockStatusHistory",
                column: "NewStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockStatusHistory_OldStatusId",
                table: "ReceivedStockStatusHistory",
                column: "OldStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStock_CurrentStatusId",
                table: "ReceivedStock",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMintStatusHistory_NewStatusId",
                table: "ExPrintMintStatusHistory",
                column: "NewStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMintStatusHistory_OldStatusId",
                table: "ExPrintMintStatusHistory",
                column: "OldStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMints_CurrentStatusId",
                table: "ExPrintMints",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedCurrencyStatusHistory_NewStatusId",
                table: "ApprovedCurrencyStatusHistory",
                column: "NewStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedCurrencyStatusHistory_OldStatusId",
                table: "ApprovedCurrencyStatusHistory",
                column: "OldStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedCurrencyProcurements_CurrentStatusId",
                table: "ApprovedCurrencyProcurements",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_FromStatusId",
                table: "WorkflowTransitions",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_ToStatusId",
                table: "WorkflowTransitions",
                column: "ToStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovedCurrencyProcurements_StatusTypes_CurrentStatusId",
                table: "ApprovedCurrencyProcurements",
                column: "CurrentStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovedCurrencyStatusHistory_StatusTypes_NewStatusId",
                table: "ApprovedCurrencyStatusHistory",
                column: "NewStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovedCurrencyStatusHistory_StatusTypes_OldStatusId",
                table: "ApprovedCurrencyStatusHistory",
                column: "OldStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExPrintMints_StatusTypes_CurrentStatusId",
                table: "ExPrintMints",
                column: "CurrentStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExPrintMintStatusHistory_StatusTypes_NewStatusId",
                table: "ExPrintMintStatusHistory",
                column: "NewStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExPrintMintStatusHistory_StatusTypes_OldStatusId",
                table: "ExPrintMintStatusHistory",
                column: "OldStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedStock_StatusTypes_CurrentStatusId",
                table: "ReceivedStock",
                column: "CurrentStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedStockStatusHistory_StatusTypes_NewStatusId",
                table: "ReceivedStockStatusHistory",
                column: "NewStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedStockStatusHistory_StatusTypes_OldStatusId",
                table: "ReceivedStockStatusHistory",
                column: "OldStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovementDiscrepancies_StatusTypes_CurrentStatusId",
                table: "StockMovementDiscrepancies",
                column: "CurrentStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_StatusTypes_CurrentStatusId",
                table: "StockMovements",
                column: "CurrentStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovementStatusHistory_StatusTypes_NewStatusId",
                table: "StockMovementStatusHistory",
                column: "NewStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovementStatusHistory_StatusTypes_OldStatusId",
                table: "StockMovementStatusHistory",
                column: "OldStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id");
        }
    }
}
