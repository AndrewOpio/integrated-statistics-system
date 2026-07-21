using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameMonthlySurplusDepositDateToEftDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('MonthlySurpluses') AND name = 'DepositDate')
                    EXEC sp_rename 'MonthlySurpluses.DepositDate', 'EftDate', 'COLUMN';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EftDate",
                table: "MonthlySurpluses",
                newName: "DepositDate");
        }
    }
}
