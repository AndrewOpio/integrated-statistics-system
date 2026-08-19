using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedBagMovementBagsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BagMovements_Bags_BagId",
                table: "BagMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_BagMovements_VaultSections_OldVaultSectionId",
                table: "BagMovements");

            migrationBuilder.AlterColumn<int>(
                name: "OldVaultSectionId",
                table: "BagMovements",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "BagId",
                table: "BagMovements",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "BagMovementBags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BagMovementId = table.Column<int>(type: "int", nullable: false),
                    BagId = table.Column<int>(type: "int", nullable: false),
                    OldVaultSectionId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BagMovementBags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BagMovementBags_BagMovements_BagMovementId",
                        column: x => x.BagMovementId,
                        principalTable: "BagMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BagMovementBags_Bags_BagId",
                        column: x => x.BagId,
                        principalTable: "Bags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BagMovementBags_VaultSections_OldVaultSectionId",
                        column: x => x.OldVaultSectionId,
                        principalTable: "VaultSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BagMovementBags_BagId",
                table: "BagMovementBags",
                column: "BagId");

            migrationBuilder.CreateIndex(
                name: "IX_BagMovementBags_BagMovementId",
                table: "BagMovementBags",
                column: "BagMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_BagMovementBags_OldVaultSectionId",
                table: "BagMovementBags",
                column: "OldVaultSectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_BagMovements_Bags_BagId",
                table: "BagMovements",
                column: "BagId",
                principalTable: "Bags",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BagMovements_VaultSections_OldVaultSectionId",
                table: "BagMovements",
                column: "OldVaultSectionId",
                principalTable: "VaultSections",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BagMovements_Bags_BagId",
                table: "BagMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_BagMovements_VaultSections_OldVaultSectionId",
                table: "BagMovements");

            migrationBuilder.DropTable(
                name: "BagMovementBags");

            migrationBuilder.AlterColumn<int>(
                name: "OldVaultSectionId",
                table: "BagMovements",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BagId",
                table: "BagMovements",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BagMovements_Bags_BagId",
                table: "BagMovements",
                column: "BagId",
                principalTable: "Bags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BagMovements_VaultSections_OldVaultSectionId",
                table: "BagMovements",
                column: "OldVaultSectionId",
                principalTable: "VaultSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
