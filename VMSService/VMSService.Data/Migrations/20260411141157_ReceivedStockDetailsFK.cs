using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReceivedStockDetailsFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedStockDetails_PackingListRanges_PackingListRangeId",
                table: "ReceivedStockDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedStockDetails_ReceivedStock_ReceivedStockId",
                table: "ReceivedStockDetails");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedStockDetails_PackingListRanges_PackingListRangeId",
                table: "ReceivedStockDetails",
                column: "PackingListRangeId",
                principalTable: "PackingListRanges",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedStockDetails_ReceivedStock_ReceivedStockId",
                table: "ReceivedStockDetails",
                column: "ReceivedStockId",
                principalTable: "ReceivedStock",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedStockDetails_PackingListRanges_PackingListRangeId",
                table: "ReceivedStockDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedStockDetails_ReceivedStock_ReceivedStockId",
                table: "ReceivedStockDetails");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedStockDetails_PackingListRanges_PackingListRangeId",
                table: "ReceivedStockDetails",
                column: "PackingListRangeId",
                principalTable: "PackingListRanges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedStockDetails_ReceivedStock_ReceivedStockId",
                table: "ReceivedStockDetails",
                column: "ReceivedStockId",
                principalTable: "ReceivedStock",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
