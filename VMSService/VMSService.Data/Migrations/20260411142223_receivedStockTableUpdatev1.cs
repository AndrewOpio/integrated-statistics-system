using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class receivedStockTableUpdatev1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedStock_ReceivedStatusTypes_ReceivedStatusTypeId",
                table: "ReceivedStock");

            migrationBuilder.DropIndex(
                name: "IX_ReceivedStock_ReceivedStatusTypeId",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "ReceivedStatus",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "ReceivedStatusTypeId",
                table: "ReceivedStock");

            migrationBuilder.AddColumn<int>(
                name: "ReceivedStatusTypesId",
                table: "ReceivedStock",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStock_ReceivedStatusTypesId",
                table: "ReceivedStock",
                column: "ReceivedStatusTypesId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedStock_ReceivedStatusTypes_ReceivedStatusTypesId",
                table: "ReceivedStock",
                column: "ReceivedStatusTypesId",
                principalTable: "ReceivedStatusTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedStock_ReceivedStatusTypes_ReceivedStatusTypesId",
                table: "ReceivedStock");

            migrationBuilder.DropIndex(
                name: "IX_ReceivedStock_ReceivedStatusTypesId",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "ReceivedStatusTypesId",
                table: "ReceivedStock");

            migrationBuilder.AddColumn<int>(
                name: "ReceivedStatus",
                table: "ReceivedStock",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReceivedStatusTypeId",
                table: "ReceivedStock",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStock_ReceivedStatusTypeId",
                table: "ReceivedStock",
                column: "ReceivedStatusTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedStock_ReceivedStatusTypes_ReceivedStatusTypeId",
                table: "ReceivedStock",
                column: "ReceivedStatusTypeId",
                principalTable: "ReceivedStatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
