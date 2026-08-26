using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchToDestructionAuthorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every existing DestructionAuthorization row in this environment belongs to Kampala (BranchId 4),
            // so that's used as the backfill default for rows that predate this column.
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "DestructionAuthorizations",
                type: "int",
                nullable: false,
                defaultValue: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "DestructionAuthorizations");
        }
    }
}
