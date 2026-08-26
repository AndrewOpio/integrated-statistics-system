using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedStockProcessingSessionNoteExaminerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockProcessingSessionNoteExaminers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockProcessingSessionId = table.Column<int>(type: "int", nullable: false),
                    NoteExaminerId = table.Column<int>(type: "int", nullable: false),
                    NumberOfBundles = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockProcessingSessionNoteExaminers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockProcessingSessionNoteExaminers_NoteExaminers_NoteExaminerId",
                        column: x => x.NoteExaminerId,
                        principalTable: "NoteExaminers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockProcessingSessionNoteExaminers_StockProcessingSessions_StockProcessingSessionId",
                        column: x => x.StockProcessingSessionId,
                        principalTable: "StockProcessingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockProcessingSessionNoteExaminers_NoteExaminerId",
                table: "StockProcessingSessionNoteExaminers",
                column: "NoteExaminerId");

            migrationBuilder.CreateIndex(
                name: "IX_StockProcessingSessionNoteExaminers_StockProcessingSessionId",
                table: "StockProcessingSessionNoteExaminers",
                column: "StockProcessingSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockProcessingSessionNoteExaminers");
        }
    }
}
