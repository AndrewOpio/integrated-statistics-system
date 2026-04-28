using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class movementLogUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NewStockMovementLogs_NewCaseFullStatusTypes_CaseFullStatusId",
                table: "NewStockMovementLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_NewStockMovementLogs_NewStockAccountingStatuses_AccountingStatusId",
                table: "NewStockMovementLogs");

            migrationBuilder.DropColumn(
                name: "NewCaseFullStatus",
                table: "NewStockMovementLogs");

            migrationBuilder.DropColumn(
                name: "NewStockAccountingStatus",
                table: "NewStockMovementLogs");

            migrationBuilder.RenameColumn(
                name: "ExpectedStockDetailId",
                table: "PackingListRanges",
                newName: "ExPrintMintDetailId");

            migrationBuilder.RenameIndex(
                name: "IX_PackingListRanges_ExpectedStockDetailId",
                table: "PackingListRanges",
                newName: "IX_PackingListRanges_ExPrintMintDetailId");

            migrationBuilder.AlterColumn<int>(
                name: "CaseFullStatusId",
                table: "NewStockMovementLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "AccountingStatusId",
                table: "NewStockMovementLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_NewStockMovementLogs_NewCaseFullStatusTypes_CaseFullStatusId",
                table: "NewStockMovementLogs",
                column: "CaseFullStatusId",
                principalTable: "NewCaseFullStatusTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NewStockMovementLogs_NewStockAccountingStatuses_AccountingStatusId",
                table: "NewStockMovementLogs",
                column: "AccountingStatusId",
                principalTable: "NewStockAccountingStatuses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NewStockMovementLogs_NewCaseFullStatusTypes_CaseFullStatusId",
                table: "NewStockMovementLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_NewStockMovementLogs_NewStockAccountingStatuses_AccountingStatusId",
                table: "NewStockMovementLogs");

            migrationBuilder.RenameColumn(
                name: "ExPrintMintDetailId",
                table: "PackingListRanges",
                newName: "ExpectedStockDetailId");

            migrationBuilder.RenameIndex(
                name: "IX_PackingListRanges_ExPrintMintDetailId",
                table: "PackingListRanges",
                newName: "IX_PackingListRanges_ExpectedStockDetailId");

            migrationBuilder.AlterColumn<int>(
                name: "CaseFullStatusId",
                table: "NewStockMovementLogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AccountingStatusId",
                table: "NewStockMovementLogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NewCaseFullStatus",
                table: "NewStockMovementLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NewStockAccountingStatus",
                table: "NewStockMovementLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_NewStockMovementLogs_NewCaseFullStatusTypes_CaseFullStatusId",
                table: "NewStockMovementLogs",
                column: "CaseFullStatusId",
                principalTable: "NewCaseFullStatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NewStockMovementLogs_NewStockAccountingStatuses_AccountingStatusId",
                table: "NewStockMovementLogs",
                column: "AccountingStatusId",
                principalTable: "NewStockAccountingStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
