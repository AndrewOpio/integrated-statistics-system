using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedBaggedBagTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentBagId",
                table: "Bags",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BaggedBags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentBagId = table.Column<int>(type: "int", nullable: false),
                    ChildBagId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaggedBags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaggedBags_Bags_ChildBagId",
                        column: x => x.ChildBagId,
                        principalTable: "Bags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaggedBags_Bags_ParentBagId",
                        column: x => x.ParentBagId,
                        principalTable: "Bags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bags_ParentBagId",
                table: "Bags",
                column: "ParentBagId");

            migrationBuilder.CreateIndex(
                name: "IX_BaggedBags_ChildBagId",
                table: "BaggedBags",
                column: "ChildBagId");

            migrationBuilder.CreateIndex(
                name: "IX_BaggedBags_ParentBagId",
                table: "BaggedBags",
                column: "ParentBagId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bags_Bags_ParentBagId",
                table: "Bags",
                column: "ParentBagId",
                principalTable: "Bags",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bags_Bags_ParentBagId",
                table: "Bags");

            migrationBuilder.DropTable(
                name: "BaggedBags");

            migrationBuilder.DropIndex(
                name: "IX_Bags_ParentBagId",
                table: "Bags");

            migrationBuilder.DropColumn(
                name: "ParentBagId",
                table: "Bags");
        }
    }
}
