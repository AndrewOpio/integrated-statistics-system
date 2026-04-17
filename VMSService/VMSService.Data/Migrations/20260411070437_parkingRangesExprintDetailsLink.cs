using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class parkingRangesExprintDetailsLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DenominationId",
                table: "PackingListRanges",
                newName: "ExPrintMintDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_PackingListRanges_ExPrintMintDetailId",
                table: "PackingListRanges",
                column: "ExPrintMintDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_PackingListRanges_ExPrintMintDetails_ExPrintMintDetailId",
                table: "PackingListRanges",
                column: "ExPrintMintDetailId",
                principalTable: "ExPrintMintDetails",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PackingListRanges_ExPrintMintDetails_ExPrintMintDetailId",
                table: "PackingListRanges");

            migrationBuilder.DropIndex(
                name: "IX_PackingListRanges_ExPrintMintDetailId",
                table: "PackingListRanges");

            migrationBuilder.RenameColumn(
                name: "ExPrintMintDetailId",
                table: "PackingListRanges",
                newName: "DenominationId");
        }
    }
}
