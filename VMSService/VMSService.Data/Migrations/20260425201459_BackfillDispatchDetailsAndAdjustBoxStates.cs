using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillDispatchDetailsAndAdjustBoxStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"
                ALTER TABLE WithdrawalRequestDenominationBoxes
                ADD CONSTRAINT CK_WRDBox_BundlesTaken_Range
                CHECK (BundlesTaken >= 0 AND BundlesTaken <= 50);
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE WithdrawalRequestDenominationBoxes
                ADD CONSTRAINT CK_WRDBox_SerialRange_Valid
                CHECK (SerialEndTaken >= SerialStartTaken);
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE WithdrawalRequestDenominationBoxes DROP CONSTRAINT CK_WRDBox_SerialRange_Valid;");
            migrationBuilder.Sql("ALTER TABLE WithdrawalRequestDenominationBoxes DROP CONSTRAINT CK_WRDBox_BundlesTaken_Range;");
        }
    }
}
