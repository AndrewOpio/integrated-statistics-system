using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class parkingRangesCleanUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PackingListDetails_PackingListRanges_PackingListRangeId1",
                table: "PackingListDetails");

            migrationBuilder.DropIndex(
                name: "IX_PackingListDetails_PackingListRangeId1",
                table: "PackingListDetails");

            migrationBuilder.DropColumn(
                name: "PackingListRangeId1",
                table: "PackingListDetails");

            migrationBuilder.AlterColumn<int>(
                name: "PackingListRangeId",
                table: "PackingListDetails",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateIndex(
                name: "IX_PackingListDetails_PackingListRangeId",
                table: "PackingListDetails",
                column: "PackingListRangeId");

            migrationBuilder.AddForeignKey(
                name: "FK_PackingListDetails_PackingListRanges_PackingListRangeId",
                table: "PackingListDetails",
                column: "PackingListRangeId",
                principalTable: "PackingListRanges",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PackingListDetails_PackingListRanges_PackingListRangeId",
                table: "PackingListDetails");

            migrationBuilder.DropIndex(
                name: "IX_PackingListDetails_PackingListRangeId",
                table: "PackingListDetails");

            migrationBuilder.AlterColumn<long>(
                name: "PackingListRangeId",
                table: "PackingListDetails",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "PackingListRangeId1",
                table: "PackingListDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PackingListDetails_PackingListRangeId1",
                table: "PackingListDetails",
                column: "PackingListRangeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PackingListDetails_PackingListRanges_PackingListRangeId1",
                table: "PackingListDetails",
                column: "PackingListRangeId1",
                principalTable: "PackingListRanges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
