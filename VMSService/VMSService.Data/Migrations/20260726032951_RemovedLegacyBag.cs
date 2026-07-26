using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovedLegacyBag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegacyBags");

            migrationBuilder.DropColumn(
                name: "LegacyBagId",
                table: "Bags");

            migrationBuilder.AddColumn<bool>(
                name: "IsLegacyDeposit",
                table: "DepositRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CfbBoxId",
                table: "BoardSessionSurplusShortages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BoardSessionSurplusShortages_CfbBoxId",
                table: "BoardSessionSurplusShortages",
                column: "CfbBoxId");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardSessionSurplusShortages_CfbBoxes_CfbBoxId",
                table: "BoardSessionSurplusShortages",
                column: "CfbBoxId",
                principalTable: "CfbBoxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardSessionSurplusShortages_CfbBoxes_CfbBoxId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropIndex(
                name: "IX_BoardSessionSurplusShortages_CfbBoxId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropColumn(
                name: "IsLegacyDeposit",
                table: "DepositRequests");

            migrationBuilder.DropColumn(
                name: "CfbBoxId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.AddColumn<int>(
                name: "LegacyBagId",
                table: "Bags",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LegacyBags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DepositDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceDepositBranchId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyBags", x => x.Id);
                });
        }
    }
}
