using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalDataset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationalDatasets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    ExtractionStartedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtractionCompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ObservationCount = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExtractedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalDatasets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationalDatasets_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StatisticalObservations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OperationalDatasetId = table.Column<int>(type: "int", nullable: false),
                    IndicatorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IndicatorName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SectionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SectionName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RecordReference = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ObservationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NumericValue = table.Column<decimal>(type: "decimal(28,6)", precision: 28, scale: 6, nullable: true),
                    TextValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DateValue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true),
                    ValueType = table.Column<int>(type: "int", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SheetName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CellReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatisticalObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatisticalObservations_OperationalDatasets_OperationalDatasetId",
                        column: x => x.OperationalDatasetId,
                        principalTable: "OperationalDatasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalDatasets_SubmissionId",
                table: "OperationalDatasets",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalDatasets_SubmissionId_IsCurrent",
                table: "OperationalDatasets",
                columns: new[] { "SubmissionId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_StatisticalObservations_OperationalDatasetId",
                table: "StatisticalObservations",
                column: "OperationalDatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_StatisticalObservations_OperationalDatasetId_IndicatorCode",
                table: "StatisticalObservations",
                columns: new[] { "OperationalDatasetId", "IndicatorCode" });

            migrationBuilder.CreateIndex(
                name: "IX_StatisticalObservations_OperationalDatasetId_ObservationDate",
                table: "StatisticalObservations",
                columns: new[] { "OperationalDatasetId", "ObservationDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StatisticalObservations");

            migrationBuilder.DropTable(
                name: "OperationalDatasets");
        }
    }
}
