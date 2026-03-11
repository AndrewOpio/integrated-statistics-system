using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedReceivedStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceivedStock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExPrintMintId = table.Column<int>(type: "int", nullable: false),
                    DateDelivered = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NumberOfPieces = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BoxNumberPrefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BoxNumberRangeStart = table.Column<int>(type: "int", nullable: true),
                    BoxNumberRangeEnd = table.Column<int>(type: "int", nullable: true),
                    SerialNumberPrefix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerialNumberRangeStart = table.Column<int>(type: "int", nullable: true),
                    SerialNumberRangeEnd = table.Column<int>(type: "int", nullable: true),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivedStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceivedStock_ExPrintMints_ExPrintMintId",
                        column: x => x.ExPrintMintId,
                        principalTable: "ExPrintMints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReceivedStock_StatusTypes_CurrentStatusId",
                        column: x => x.CurrentStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStock_CurrentStatusId",
                table: "ReceivedStock",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStock_ExPrintMintId",
                table: "ReceivedStock",
                column: "ExPrintMintId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceivedStock");
        }
    }
}
