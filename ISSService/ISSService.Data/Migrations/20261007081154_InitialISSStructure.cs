using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialISSStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StatisticalProcesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProcessType = table.Column<int>(type: "int", nullable: false),
                    CollectionMode = table.Column<int>(type: "int", nullable: false),
                    ReportingFrequency = table.Column<int>(type: "int", nullable: false),
                    ResponsibleUnit = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatisticalProcesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportingCycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatisticalProcessId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PeriodStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CollectionOpenDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmissionDueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CollectionCloseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportingCycles_StatisticalProcesses_StatisticalProcessId",
                        column: x => x.StatisticalProcessId,
                        principalTable: "StatisticalProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportingCycles_StatisticalProcessId_Code",
                table: "ReportingCycles",
                columns: new[] { "StatisticalProcessId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatisticalProcesses_Code",
                table: "StatisticalProcesses",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportingCycles");

            migrationBuilder.DropTable(
                name: "StatisticalProcesses");
        }
    }
}
