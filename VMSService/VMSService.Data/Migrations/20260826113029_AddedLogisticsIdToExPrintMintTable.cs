using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedLogisticsIdToExPrintMintTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsWitness",
                table: "DestructionRequestMembers");

            migrationBuilder.RenameColumn(
                name: "VerificationOfficer",
                table: "DestructionRequests",
                newName: "VerificationOfficerUserId");

            migrationBuilder.AddColumn<int>(
                name: "LogisticsId",
                table: "ExPrintMints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "DestructionAuthorizations",
                type: "int",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "DestructionAuthorizations");

            migrationBuilder.RenameColumn(
                name: "VerificationOfficerUserId",
                table: "DestructionRequests",
                newName: "VerificationOfficer");

            migrationBuilder.AddColumn<bool>(
                name: "IsWitness",
                table: "DestructionRequestMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
