using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeTransactionRefNumberNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WithdrawalRequests_TransactionRefNumber",
                table: "WithdrawalRequests");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionRefNumber",
                table: "WithdrawalRequests",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_TransactionRefNumber",
                table: "WithdrawalRequests",
                column: "TransactionRefNumber",
                unique: true,
                filter: "[TransactionRefNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WithdrawalRequests_TransactionRefNumber",
                table: "WithdrawalRequests");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionRefNumber",
                table: "WithdrawalRequests",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_TransactionRefNumber",
                table: "WithdrawalRequests",
                column: "TransactionRefNumber",
                unique: true);
        }
    }
}
