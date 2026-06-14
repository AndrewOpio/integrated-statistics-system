using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class addDestructionLocationFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DestructionLocationId",
                table: "DestructionRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DestructionLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestructionLocations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DestructionRequests_DestructionLocationId",
                table: "DestructionRequests",
                column: "DestructionLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_DestructionRequests_DestructionLocations_DestructionLocationId",
                table: "DestructionRequests",
                column: "DestructionLocationId",
                principalTable: "DestructionLocations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DestructionRequests_DestructionLocations_DestructionLocationId",
                table: "DestructionRequests");

            migrationBuilder.DropTable(
                name: "DestructionLocations");

            migrationBuilder.DropIndex(
                name: "IX_DestructionRequests_DestructionLocationId",
                table: "DestructionRequests");

            migrationBuilder.DropColumn(
                name: "DestructionLocationId",
                table: "DestructionRequests");
        }
    }
}
