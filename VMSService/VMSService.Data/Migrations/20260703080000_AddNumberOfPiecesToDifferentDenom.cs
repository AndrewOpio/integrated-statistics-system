using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    //[Migration("20260703080000_AddNumberOfPiecesToDifferentDenom")]
    public partial class AddNumberOfPiecesToDifferentDenom : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NumberOfPieces",
                table: "BoardSessionDifferentDenoms",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumberOfPieces",
                table: "BoardSessionDifferentDenoms");
        }
    }
}
