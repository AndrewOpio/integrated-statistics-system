using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class recievedDetailsDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedStockDetails_ReceivedStock_ReceivedStockId",
                table: "ReceivedStockDetails");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedStockDetails_ReceivedStock_ReceivedStockId",
                table: "ReceivedStockDetails",
                column: "ReceivedStockId",
                principalTable: "ReceivedStock",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedStockDetails_ReceivedStock_ReceivedStockId",
                table: "ReceivedStockDetails");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedStockDetails_ReceivedStock_ReceivedStockId",
                table: "ReceivedStockDetails",
                column: "ReceivedStockId",
                principalTable: "ReceivedStock",
                principalColumn: "Id");
        }
    }
}
