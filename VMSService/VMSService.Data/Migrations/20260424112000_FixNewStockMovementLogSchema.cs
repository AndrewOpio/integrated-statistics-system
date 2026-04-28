using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixNewStockMovementLogSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the duplicate columns and make FK fields nullable
            migrationBuilder.DropColumn(
                name: "NewCaseFullStatus",
                table: "NewStockMovementLogs");

            migrationBuilder.DropColumn(
                name: "NewStockAccountingStatus",
                table: "NewStockMovementLogs");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "AccountingStatusId",
                table: "NewStockMovementLogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CaseFullStatusId",
                table: "NewStockMovementLogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NewStockAccountingStatus",
                table: "NewStockMovementLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NewCaseFullStatus",
                table: "NewStockMovementLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
