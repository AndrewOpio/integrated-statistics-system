using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLogisticsToExPrintMint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LogisticsId",
                table: "ExPrintMints",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMints_LogisticsId",
                table: "ExPrintMints",
                column: "LogisticsId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExPrintMints_Logistics_LogisticsId",
                table: "ExPrintMints",
                column: "LogisticsId",
                principalTable: "Logistics",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExPrintMints_Logistics_LogisticsId",
                table: "ExPrintMints");

            migrationBuilder.DropIndex(
                name: "IX_ExPrintMints_LogisticsId",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "LogisticsId",
                table: "ExPrintMints");
        }
    }
}
