using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VMSService.Data.Configurations;

#nullable disable

namespace VMSService.Data.Migrations
{
    [DbContext(typeof(VMSServiceDbContext))]
    [Migration("20260627235900_RestructureMonthlyShortageDetails")]
    public partial class RestructureMonthlyShortageDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "BranchId", table: "MonthlyShortages");

            migrationBuilder.CreateTable(
                name: "MonthlyShortageDetails",
                columns: table => new
                {
                    Id                = table.Column<int>(type: "int", nullable: false)
                                            .Annotation("SqlServer:Identity", "1, 1"),
                    MonthlyShortageId = table.Column<int>(type: "int", nullable: false),
                    BranchId          = table.Column<int>(type: "int", nullable: false),
                    TotalSurplus      = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalShortage     = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetShortage       = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyShortageDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyShortageDetails_MonthlyShortages_MonthlyShortageId",
                        column: x => x.MonthlyShortageId,
                        principalTable: "MonthlyShortages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyShortageDetails_MonthlyShortageId",
                table: "MonthlyShortageDetails",
                column: "MonthlyShortageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MonthlyShortageDetails");
            migrationBuilder.AddColumn<int>(name: "BranchId", table: "MonthlyShortages",
                type: "int", nullable: false, defaultValue: 0);
        }
    }
}
