using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRespondentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Respondents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    RespondentType = table.Column<int>(type: "int", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PrimaryContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PrimaryContactEmail = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PrimaryContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Respondents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RespondentCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportingCycleId = table.Column<int>(type: "int", nullable: false),
                    RespondentId = table.Column<int>(type: "int", nullable: false),
                    CaseReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DistributedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FirstSubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LatestSubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespondentCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RespondentCases_ReportingCycles_ReportingCycleId",
                        column: x => x.ReportingCycleId,
                        principalTable: "ReportingCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RespondentCases_Respondents_RespondentId",
                        column: x => x.RespondentId,
                        principalTable: "Respondents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RespondentCases_CaseReference",
                table: "RespondentCases",
                column: "CaseReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RespondentCases_ReportingCycleId_RespondentId",
                table: "RespondentCases",
                columns: new[] { "ReportingCycleId", "RespondentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RespondentCases_RespondentId",
                table: "RespondentCases",
                column: "RespondentId");

            migrationBuilder.CreateIndex(
                name: "IX_Respondents_Code",
                table: "Respondents",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RespondentCases");

            migrationBuilder.DropTable(
                name: "Respondents");
        }
    }
}
