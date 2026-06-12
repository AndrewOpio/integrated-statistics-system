using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AttachmentIdColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentIds",
                table: "StockMovementStatusHistory",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentIds",
                table: "ReceivedStockStatusHistory",
                type: "nvarchar(max)",
                nullable: true);

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
                table: "AgentDetailStatusHistory",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentIds",
                table: "StockMovementStatusHistory");

            migrationBuilder.DropColumn(
                name: "AttachmentIds",
                table: "ReceivedStockStatusHistory");

            migrationBuilder.DropColumn(
                name: "AttachmentIds",
                table: "ExPrintMintStatusHistory");

            migrationBuilder.DropColumn(
                name: "AttachmentIds",
                table: "ApprovedCurrencyStatusHistory");

            migrationBuilder.DropColumn(
                name: "AttachmentIds",
                table: "AgentDetailStatusHistory");
        }
    }
}
