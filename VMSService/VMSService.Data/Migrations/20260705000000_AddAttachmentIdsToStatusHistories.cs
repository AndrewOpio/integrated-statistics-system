using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentIdsToStatusHistories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentIds",
                table: "ExPrintMintStatusHistory",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentIds",
                table: "ApprovedCurrencyStatusHistory",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentIds",
                table: "ReceivedStockStatusHistory",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentIds",
                table: "ExPrintMintStatusHistory");

            migrationBuilder.DropColumn(
                name: "AttachmentIds",
                table: "ApprovedCurrencyStatusHistory");

            migrationBuilder.DropColumn(
                name: "AttachmentIds",
                table: "ReceivedStockStatusHistory");
        }
    }
}
