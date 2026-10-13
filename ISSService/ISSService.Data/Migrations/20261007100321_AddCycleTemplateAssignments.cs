using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCycleTemplateAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CycleTemplateAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportingCycleId = table.Column<int>(type: "int", nullable: false),
                    DataCollectionTemplateVersionId = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CycleTemplateAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CycleTemplateAssignments_DataCollectionTemplateVersions_DataCollectionTemplateVersionId",
                        column: x => x.DataCollectionTemplateVersionId,
                        principalTable: "DataCollectionTemplateVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CycleTemplateAssignments_ReportingCycles_ReportingCycleId",
                        column: x => x.ReportingCycleId,
                        principalTable: "ReportingCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CycleTemplateAssignments_DataCollectionTemplateVersionId",
                table: "CycleTemplateAssignments",
                column: "DataCollectionTemplateVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CycleTemplateAssignments_ReportingCycleId_DataCollectionTemplateVersionId",
                table: "CycleTemplateAssignments",
                columns: new[] { "ReportingCycleId", "DataCollectionTemplateVersionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CycleTemplateAssignments");
        }
    }
}
