using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertVerificationOfficerToUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"EXEC sp_rename 'DestructionRequests.VerificationOfficer', 'VerificationOfficerUserId', 'COLUMN';");

            // Existing rows only ever held the free-typed name "Andrew Opio" (the account that entered
            // this test data), not a real Verification Officer. "vokampala" is the only account that
            // actually holds the Verification Officer role, so historical rows are backfilled to it
            // rather than left pointing at a name with no matching account.
            migrationBuilder.Sql(@"UPDATE DestructionRequests SET VerificationOfficerUserId = 'vokampala';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"EXEC sp_rename 'DestructionRequests.VerificationOfficerUserId', 'VerificationOfficer', 'COLUMN';");
        }
    }
}
