using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddValidationManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ValidationRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    RunNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalChecks = table.Column<int>(type: "int", nullable: false),
                    PassedChecks = table.Column<int>(type: "int", nullable: false),
                    WarningChecks = table.Column<int>(type: "int", nullable: false),
                    FailedChecks = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExecutedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationRuns_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ValidationFindings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ValidationRunId = table.Column<int>(type: "int", nullable: false),
                    RuleCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RuleName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SheetName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    FieldName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CellReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RecordReference = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ObservedValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExpectedValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResolvedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationFindings_ValidationRuns_ValidationRunId",
                        column: x => x.ValidationRunId,
                        principalTable: "ValidationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ValidationFindings_ValidationRunId",
                table: "ValidationFindings",
                column: "ValidationRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationFindings_ValidationRunId_RuleCode",
                table: "ValidationFindings",
                columns: new[] { "ValidationRunId", "RuleCode" });

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRuns_SubmissionId_RunNumber",
                table: "ValidationRuns",
                columns: new[] { "SubmissionId", "RunNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ValidationFindings");

            migrationBuilder.DropTable(
                name: "ValidationRuns");
        }
    }
}
