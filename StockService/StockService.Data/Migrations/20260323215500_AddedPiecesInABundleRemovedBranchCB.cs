using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockService.Data.Migrations
{
    public partial class AddedPiecesInABundleRemovedBranchCB : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BranchCategories_Branches_BranchId",
                table: "BranchCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_StockLedger_Branches_StoreId",
                table: "StockLedger");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "CommercialBanks");

            migrationBuilder.DropIndex(
                name: "IX_StockLedger_StoreId",
                table: "StockLedger");

            migrationBuilder.DropIndex(
                name: "IX_BranchCategories_BranchId",
                table: "BranchCategories");

            migrationBuilder.AddColumn<int>(
                name: "NumberOfPiecesInABundle",
                table: "Denominations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberOfPiecesInABundle",
                table: "Denominations");

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommercialBanks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercialBanks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockLedger_StoreId",
                table: "StockLedger",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchCategories_BranchId",
                table: "BranchCategories",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_BranchCategories_Branches_BranchId",
                table: "BranchCategories",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockLedger_Branches_StoreId",
                table: "StockLedger",
                column: "StoreId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
