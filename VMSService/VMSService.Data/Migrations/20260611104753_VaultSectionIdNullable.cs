using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class VaultSectionIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bags_VaultSections_VaultSectionId",
                table: "Bags");

            migrationBuilder.AlterColumn<int>(
                name: "VaultSectionId",
                table: "Bags",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Bags_VaultSections_VaultSectionId",
                table: "Bags",
                column: "VaultSectionId",
                principalTable: "VaultSections",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bags_VaultSections_VaultSectionId",
                table: "Bags");

            migrationBuilder.AlterColumn<int>(
                name: "VaultSectionId",
                table: "Bags",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bags_VaultSections_VaultSectionId",
                table: "Bags",
                column: "VaultSectionId",
                principalTable: "VaultSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
