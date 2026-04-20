using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameExprintMintDetailsIdToExpectedStockDetailsId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration adds the ExpectedStockDetailId column to ReceivedStockDetails
            // if it doesn't already exist, establishing a relationship to ExPrintMintDetails
            
            // Check if the column exists and add it if it doesn't
            migrationBuilder.AddColumn<int>(
                name: "ExpectedStockDetailId",
                table: "ReceivedStockDetails",
                type: "int",
                nullable: true);

            // Create an index on the new column
            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockDetails_ExpectedStockDetailId",
                table: "ReceivedStockDetails",
                column: "ExpectedStockDetailId");

            // Add the foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedStockDetails_ExPrintMintDetails_ExpectedStockDetailId",
                table: "ReceivedStockDetails",
                column: "ExpectedStockDetailId",
                principalTable: "ExPrintMintDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the changes
            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedStockDetails_ExPrintMintDetails_ExpectedStockDetailId",
                table: "ReceivedStockDetails");

            migrationBuilder.DropIndex(
                name: "IX_ReceivedStockDetails_ExpectedStockDetailId",
                table: "ReceivedStockDetails");

            migrationBuilder.DropColumn(
                name: "ExpectedStockDetailId",
                table: "ReceivedStockDetails");
        }
    }
}


