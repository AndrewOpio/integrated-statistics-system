using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBpsXmlImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BpsXmlImports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MachineSerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SiteCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileGeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DenominationValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FitPieces = table.Column<int>(type: "int", nullable: false),
                    UnFitShredPieces = table.Column<int>(type: "int", nullable: false),
                    FitShredPieces = table.Column<int>(type: "int", nullable: false),
                    RejectPieces = table.Column<int>(type: "int", nullable: false),
                    FitValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnFitShredValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FitShredValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StockProcessingSessionId = table.Column<int>(type: "int", nullable: false),
                    StockProcessingResultId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BpsXmlImports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BpsXmlImports_StockProcessingResults_StockProcessingResultId",
                        column: x => x.StockProcessingResultId,
                        principalTable: "StockProcessingResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BpsXmlImports_StockProcessingSessions_StockProcessingSessionId",
                        column: x => x.StockProcessingSessionId,
                        principalTable: "StockProcessingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BpsXmlImports_FileName",
                table: "BpsXmlImports",
                column: "FileName",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BpsXmlImports_StockProcessingResultId",
                table: "BpsXmlImports",
                column: "StockProcessingResultId");

            migrationBuilder.CreateIndex(
                name: "IX_BpsXmlImports_StockProcessingSessionId",
                table: "BpsXmlImports",
                column: "StockProcessingSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BpsXmlImports");
        }
    }
}
