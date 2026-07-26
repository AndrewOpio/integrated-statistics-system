using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class appliedPenalties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppliedPenalties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PenaltyId = table.Column<int>(type: "int", nullable: false),
                    ChargeValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DenomValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    IsSettledOnRtgs = table.Column<bool>(type: "bit", nullable: false),
                    RtgsCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RtgsSettlingMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppliedPenalties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppliedPenalties_Penalties_PenaltyId",
                        column: x => x.PenaltyId,
                        principalTable: "Penalties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppliedPenalties_SourceId_PenaltyId",
                table: "AppliedPenalties",
                columns: new[] { "SourceId", "PenaltyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppliedPenalties_PenaltyId",
                table: "AppliedPenalties",
                column: "PenaltyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AppliedPenalties");
        }
    }
}
