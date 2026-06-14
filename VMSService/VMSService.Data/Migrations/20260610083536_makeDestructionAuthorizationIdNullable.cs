using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class makeDestructionAuthorizationIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DestructionRequests_DestructionAuthorizations_DestructionAuthorizationId",
                table: "DestructionRequests");

            migrationBuilder.AlterColumn<int>(
                name: "DestructionAuthorizationId",
                table: "DestructionRequests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_DestructionRequests_DestructionAuthorizations_DestructionAuthorizationId",
                table: "DestructionRequests",
                column: "DestructionAuthorizationId",
                principalTable: "DestructionAuthorizations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DestructionRequests_DestructionAuthorizations_DestructionAuthorizationId",
                table: "DestructionRequests");

            migrationBuilder.AlterColumn<int>(
                name: "DestructionAuthorizationId",
                table: "DestructionRequests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DestructionRequests_DestructionAuthorizations_DestructionAuthorizationId",
                table: "DestructionRequests",
                column: "DestructionAuthorizationId",
                principalTable: "DestructionAuthorizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
