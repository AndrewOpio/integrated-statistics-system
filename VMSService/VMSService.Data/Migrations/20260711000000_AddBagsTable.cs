using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBagsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bags",
                columns: table => new
                {
                    Id             = table.Column<int>(type: "int", nullable: false)
                                         .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DenominationId  = table.Column<int>(type: "int", nullable: false),
                    BranchId        = table.Column<int>(type: "int", nullable: false),
                    CbCode          = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VaultSectionId  = table.Column<int>(type: "int", nullable: true),
                    CategoryId      = table.Column<int>(type: "int", nullable: true),
                    SubCategoryId   = table.Column<int>(type: "int", nullable: true),
                    IsProcessed     = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsDeleted       = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedBy       = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "system"),
                    CreatedDate     = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy      = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "system"),
                    ModifiedDate    = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bags", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bags_DenominationId_BranchId",
                table: "Bags",
                columns: new[] { "DenominationId", "BranchId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Bags");
        }
    }
}
