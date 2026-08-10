using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    public partial class AddStockMovementDenominationBags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockMovementDenominationBags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockMovementDenominationId = table.Column<int>(type: "int", nullable: false),
                    BagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovementDenominationBags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovementDenominationBags_StockMovementDenominations",
                        column: x => x.StockMovementDenominationId,
                        principalTable: "StockMovementDenominations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockMovementDenominationBags_Bags",
                        column: x => x.BagId,
                        principalTable: "Bags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementDenominationBags_StockMovementDenominationId",
                table: "StockMovementDenominationBags",
                column: "StockMovementDenominationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementDenominationBags_BagId",
                table: "StockMovementDenominationBags",
                column: "BagId");

            migrationBuilder.DropColumn(
                name: "BagIds",
                table: "StockMovementDenominations");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "StockMovementDenominationBags");

            migrationBuilder.AddColumn<string>(
                name: "BagIds",
                table: "StockMovementDenominations",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
