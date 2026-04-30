using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorkflowService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RequiredStatusId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RequiredRole",
                table: "WorkflowTransitions",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "RequiredStatusId",
                table: "WorkflowTransitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_RequiredStatusId",
                table: "WorkflowTransitions",
                column: "RequiredStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowTransitions_StatusTypes_RequiredStatusId",
                table: "WorkflowTransitions",
                column: "RequiredStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowTransitions_StatusTypes_RequiredStatusId",
                table: "WorkflowTransitions");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowTransitions_RequiredStatusId",
                table: "WorkflowTransitions");

            migrationBuilder.DropColumn(
                name: "RequiredStatusId",
                table: "WorkflowTransitions");

            migrationBuilder.AlterColumn<string>(
                name: "RequiredRole",
                table: "WorkflowTransitions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
