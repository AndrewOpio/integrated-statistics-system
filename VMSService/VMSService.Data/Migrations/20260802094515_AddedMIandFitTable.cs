using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedMIandFitTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MIandFits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockProcessingPlanId = table.Column<int>(type: "int", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    IssuedReIssueValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IssuedCanceledValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReturnedReIssueValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReturnedCanceledValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BoardSupervisorUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MIandFits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MIandFits_StockProcessingPlans_StockProcessingPlanId",
                        column: x => x.StockProcessingPlanId,
                        principalTable: "StockProcessingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MIandFits_StockProcessingPlanId",
                table: "MIandFits",
                column: "StockProcessingPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MIandFits");
        }
    }
}
