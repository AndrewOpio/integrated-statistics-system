using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedReceivedStockStatusHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceivedStockStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceivedStockId = table.Column<int>(type: "int", nullable: false),
                    OldStatusId = table.Column<int>(type: "int", nullable: false),
                    NewStatusId = table.Column<int>(type: "int", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivedStockStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceivedStockStatusHistory_ReceivedStock_ReceivedStockId",
                        column: x => x.ReceivedStockId,
                        principalTable: "ReceivedStock",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ReceivedStockStatusHistory_StatusTypes_NewStatusId",
                        column: x => x.NewStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ReceivedStockStatusHistory_StatusTypes_OldStatusId",
                        column: x => x.OldStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockStatusHistory_NewStatusId",
                table: "ReceivedStockStatusHistory",
                column: "NewStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockStatusHistory_OldStatusId",
                table: "ReceivedStockStatusHistory",
                column: "OldStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockStatusHistory_ReceivedStockId",
                table: "ReceivedStockStatusHistory",
                column: "ReceivedStockId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceivedStockStatusHistory");
        }
    }
}
