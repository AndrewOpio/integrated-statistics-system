using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class serialNumberRanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SerialNumberRange",
                table: "StockMovementDenominations");

            migrationBuilder.CreateTable(
                name: "StockMovementDenominationSerialNumberRanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Prefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Start = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    End = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StockMovementDenominationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovementDenominationSerialNumberRanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovementDenominationSerialNumberRanges_StockMovementDenominations_StockMovementDenominationId",
                        column: x => x.StockMovementDenominationId,
                        principalTable: "StockMovementDenominations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementDenominationSerialNumberRanges_StockMovementDenominationId",
                table: "StockMovementDenominationSerialNumberRanges",
                column: "StockMovementDenominationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockMovementDenominationSerialNumberRanges");

            migrationBuilder.AddColumn<string>(
                name: "SerialNumberRange",
                table: "StockMovementDenominations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
