using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class stockMovementTableUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "StockMovements",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "StockMovements",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CurrentStatusId",
                table: "StockMovements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "StockMovements",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "StockMovements",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "StockMovementStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockMovementId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_StockMovementStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovementStatusHistory_StatusTypes_NewStatusId",
                        column: x => x.NewStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMovementStatusHistory_StatusTypes_OldStatusId",
                        column: x => x.OldStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMovementStatusHistory_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CurrentStatusId",
                table: "StockMovements",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementStatusHistory_NewStatusId",
                table: "StockMovementStatusHistory",
                column: "NewStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementStatusHistory_OldStatusId",
                table: "StockMovementStatusHistory",
                column: "OldStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementStatusHistory_StockMovementId",
                table: "StockMovementStatusHistory",
                column: "StockMovementId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_StatusTypes_CurrentStatusId",
                table: "StockMovements",
                column: "CurrentStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_StatusTypes_CurrentStatusId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "StockMovementStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_CurrentStatusId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "CurrentStatusId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "StockMovements");
        }
    }
}
