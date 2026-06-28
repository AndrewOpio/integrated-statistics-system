using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveParentBagId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bags_Bags_ParentBagId",
                table: "Bags");

            migrationBuilder.RenameColumn(
                name: "ParentBagId",
                table: "Bags",
                newName: "BagId");

            migrationBuilder.RenameIndex(
                name: "IX_Bags_ParentBagId",
                table: "Bags",
                newName: "IX_Bags_BagId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bags_Bags_BagId",
                table: "Bags",
                column: "BagId",
                principalTable: "Bags",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bags_Bags_BagId",
                table: "Bags");

            migrationBuilder.RenameColumn(
                name: "BagId",
                table: "Bags",
                newName: "ParentBagId");

            migrationBuilder.RenameIndex(
                name: "IX_Bags_BagId",
                table: "Bags",
                newName: "IX_Bags_ParentBagId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bags_Bags_ParentBagId",
                table: "Bags",
                column: "ParentBagId",
                principalTable: "Bags",
                principalColumn: "Id");
        }
    }
}
