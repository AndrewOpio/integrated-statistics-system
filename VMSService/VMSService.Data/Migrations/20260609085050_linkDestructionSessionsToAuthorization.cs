using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class linkDestructionSessionsToAuthorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove orphaned sessions and their children before adding the FK
            migrationBuilder.Sql("DELETE FROM DestructionRequestStatusHistory");
            migrationBuilder.Sql("DELETE FROM DestructionRequestMembers");
            migrationBuilder.Sql("DELETE FROM DestructionRequestDenominations");
            migrationBuilder.Sql("DELETE FROM DestructionRequests");

            migrationBuilder.AddColumn<int>(
                name: "DestructionAuthorizationId",
                table: "DestructionRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DestructionRequests_DestructionAuthorizationId",
                table: "DestructionRequests",
                column: "DestructionAuthorizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_DestructionRequests_DestructionAuthorizations_DestructionAuthorizationId",
                table: "DestructionRequests",
                column: "DestructionAuthorizationId",
                principalTable: "DestructionAuthorizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DestructionRequests_DestructionAuthorizations_DestructionAuthorizationId",
                table: "DestructionRequests");

            migrationBuilder.DropIndex(
                name: "IX_DestructionRequests_DestructionAuthorizationId",
                table: "DestructionRequests");

            migrationBuilder.DropColumn(
                name: "DestructionAuthorizationId",
                table: "DestructionRequests");
        }
    }
}
