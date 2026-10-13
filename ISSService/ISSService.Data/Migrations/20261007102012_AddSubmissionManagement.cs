using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RespondentCaseId = table.Column<int>(type: "int", nullable: false),
                    SubmissionNumber = table.Column<int>(type: "int", nullable: false),
                    SubmissionMethod = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Submissions_RespondentCases_RespondentCaseId",
                        column: x => x.RespondentCaseId,
                        principalTable: "RespondentCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_RespondentCaseId_SubmissionNumber",
                table: "Submissions",
                columns: new[] { "RespondentCaseId", "SubmissionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Submissions");
        }
    }
}
