using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlignRespondentCaseWithWorkflowService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "RespondentCases",
                newName: "CurrentStatusId");

            migrationBuilder.AddColumn<string>(
                name: "CurrentStatusName",
                table: "RespondentCases",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RespondentCaseStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RespondentCaseId = table.Column<int>(type: "int", nullable: false),
                    WorkflowTransitionId = table.Column<int>(type: "int", nullable: true),
                    OldStatusId = table.Column<int>(type: "int", nullable: false),
                    OldStatusName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NewStatusId = table.Column<int>(type: "int", nullable: false),
                    NewStatusName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespondentCaseStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RespondentCaseStatusHistories_RespondentCases_RespondentCaseId",
                        column: x => x.RespondentCaseId,
                        principalTable: "RespondentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RespondentCases_CurrentStatusId",
                table: "RespondentCases",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_RespondentCaseStatusHistories_RespondentCaseId",
                table: "RespondentCaseStatusHistories",
                column: "RespondentCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_RespondentCaseStatusHistories_RespondentCaseId_ChangedDate",
                table: "RespondentCaseStatusHistories",
                columns: new[] { "RespondentCaseId", "ChangedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RespondentCaseStatusHistories_WorkflowTransitionId",
                table: "RespondentCaseStatusHistories",
                column: "WorkflowTransitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RespondentCaseStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_RespondentCases_CurrentStatusId",
                table: "RespondentCases");

            migrationBuilder.DropColumn(
                name: "CurrentStatusName",
                table: "RespondentCases");

            migrationBuilder.RenameColumn(
                name: "CurrentStatusId",
                table: "RespondentCases",
                newName: "Status");
        }
    }
}
