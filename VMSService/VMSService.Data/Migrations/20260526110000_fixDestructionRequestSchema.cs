using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class fixDestructionRequestSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the phantom FK and column created by the original broken migration
            migrationBuilder.DropForeignKey(
                name: "FK_DestructionRequests_DestructionTypes_DestructionTypeId1",
                table: "DestructionRequests");

            migrationBuilder.DropIndex(
                name: "IX_DestructionRequests_DestructionTypeId1",
                table: "DestructionRequests");

            migrationBuilder.DropColumn(
                name: "DestructionTypeId1",
                table: "DestructionRequests");

            // Fix DestructionTypeId from nvarchar to int
            migrationBuilder.AlterColumn<int>(
                name: "DestructionTypeId",
                table: "DestructionRequests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Add correct index and FK on DestructionTypeId
            migrationBuilder.CreateIndex(
                name: "IX_DestructionRequests_DestructionTypeId",
                table: "DestructionRequests",
                column: "DestructionTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_DestructionRequests_DestructionTypes_DestructionTypeId",
                table: "DestructionRequests",
                column: "DestructionTypeId",
                principalTable: "DestructionTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Fix Amount from nvarchar to decimal
            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "DestructionRequestDenominations",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DestructionRequests_DestructionTypes_DestructionTypeId",
                table: "DestructionRequests");

            migrationBuilder.DropIndex(
                name: "IX_DestructionRequests_DestructionTypeId",
                table: "DestructionRequests");

            migrationBuilder.AlterColumn<string>(
                name: "DestructionTypeId",
                table: "DestructionRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Amount",
                table: "DestructionRequestDenominations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<int>(
                name: "DestructionTypeId1",
                table: "DestructionRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DestructionRequests_DestructionTypeId1",
                table: "DestructionRequests",
                column: "DestructionTypeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_DestructionRequests_DestructionTypes_DestructionTypeId1",
                table: "DestructionRequests",
                column: "DestructionTypeId1",
                principalTable: "DestructionTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
