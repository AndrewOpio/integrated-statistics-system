using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ISSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataCollectionTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatisticalProcessId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TemplateType = table.Column<int>(type: "int", nullable: false),
                    IsRespondentSpecific = table.Column<bool>(type: "bit", nullable: false),
                    SupportsPreviousPeriodData = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataCollectionTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataCollectionTemplates_StatisticalProcesses_StatisticalProcessId",
                        column: x => x.StatisticalProcessId,
                        principalTable: "StatisticalProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DataCollectionTemplateVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataCollectionTemplateId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    VersionLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ChangeNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataCollectionTemplateVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataCollectionTemplateVersions_DataCollectionTemplates_DataCollectionTemplateId",
                        column: x => x.DataCollectionTemplateId,
                        principalTable: "DataCollectionTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataCollectionTemplates_StatisticalProcessId_Code",
                table: "DataCollectionTemplates",
                columns: new[] { "StatisticalProcessId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataCollectionTemplateVersions_DataCollectionTemplateId_VersionNumber",
                table: "DataCollectionTemplateVersions",
                columns: new[] { "DataCollectionTemplateId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataCollectionTemplateVersions");

            migrationBuilder.DropTable(
                name: "DataCollectionTemplates");
        }
    }
}
