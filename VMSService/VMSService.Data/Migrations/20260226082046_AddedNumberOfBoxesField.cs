using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedNumberOfBoxesField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumberOfBoxes",
                table: "ReceivedStock",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfBoxes",
                table: "ExPrintMints",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyVendorId",
                table: "ApprovedCurrencyDenominations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedCurrencyDenominations_CurrencyVendorId",
                table: "ApprovedCurrencyDenominations",
                column: "CurrencyVendorId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovedCurrencyDenominations_CurrencyVendors_CurrencyVendorId",
                table: "ApprovedCurrencyDenominations",
                column: "CurrencyVendorId",
                principalTable: "CurrencyVendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovedCurrencyDenominations_CurrencyVendors_CurrencyVendorId",
                table: "ApprovedCurrencyDenominations");

            migrationBuilder.DropIndex(
                name: "IX_ApprovedCurrencyDenominations_CurrencyVendorId",
                table: "ApprovedCurrencyDenominations");

            migrationBuilder.DropColumn(
                name: "NumberOfBoxes",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "NumberOfBoxes",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "CurrencyVendorId",
                table: "ApprovedCurrencyDenominations");
        }
    }
}
