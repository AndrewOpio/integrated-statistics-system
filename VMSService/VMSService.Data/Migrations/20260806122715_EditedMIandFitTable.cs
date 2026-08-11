using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class EditedMIandFitTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Pairings are set explicitly: EF cannot tell a rename from a drop+add, and with four
            // decimal(18,2) columns renamed at once it paired them the wrong way round (Re-Issue -> UnFit,
            // Canceled -> Fit), which would have moved existing data into the inverted column.
            migrationBuilder.RenameColumn(
                name: "ReturnedReIssueValue",
                table: "MIandFits",
                newName: "ReturnedFitValue");

            migrationBuilder.RenameColumn(
                name: "ReturnedCanceledValue",
                table: "MIandFits",
                newName: "ReturnedUnFitValue");

            migrationBuilder.RenameColumn(
                name: "IssuedReIssueValue",
                table: "MIandFits",
                newName: "IssuedFitValue");

            migrationBuilder.RenameColumn(
                name: "IssuedCanceledValue",
                table: "MIandFits",
                newName: "IssuedUnFitValue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReturnedFitValue",
                table: "MIandFits",
                newName: "ReturnedReIssueValue");

            migrationBuilder.RenameColumn(
                name: "ReturnedUnFitValue",
                table: "MIandFits",
                newName: "ReturnedCanceledValue");

            migrationBuilder.RenameColumn(
                name: "IssuedFitValue",
                table: "MIandFits",
                newName: "IssuedReIssueValue");

            migrationBuilder.RenameColumn(
                name: "IssuedUnFitValue",
                table: "MIandFits",
                newName: "IssuedCanceledValue");
        }
    }
}
