using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchDetailsToWithdrawalRequestDenominationBox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BoxNo",
                table: "WithdrawalRequestDenominationBoxes",
                newName: "CaseNumber");

            migrationBuilder.RenameIndex(
                name: "IX_WithdrawalRequestDenominationBoxes_BoxNo",
                table: "WithdrawalRequestDenominationBoxes",
                newName: "IX_WithdrawalRequestDenominationBoxes_CaseNumber");

            migrationBuilder.AddColumn<int>(
                name: "BundlesTaken",
                table: "WithdrawalRequestDenominationBoxes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "SerialEndTaken",
                table: "WithdrawalRequestDenominationBoxes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "SerialStartTaken",
                table: "WithdrawalRequestDenominationBoxes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BundlesTaken",
                table: "WithdrawalRequestDenominationBoxes");

            migrationBuilder.DropColumn(
                name: "SerialEndTaken",
                table: "WithdrawalRequestDenominationBoxes");

            migrationBuilder.DropColumn(
                name: "SerialStartTaken",
                table: "WithdrawalRequestDenominationBoxes");

            migrationBuilder.RenameColumn(
                name: "CaseNumber",
                table: "WithdrawalRequestDenominationBoxes",
                newName: "BoxNo");

            migrationBuilder.RenameIndex(
                name: "IX_WithdrawalRequestDenominationBoxes_CaseNumber",
                table: "WithdrawalRequestDenominationBoxes",
                newName: "IX_WithdrawalRequestDenominationBoxes_BoxNo");
        }
    }
}
