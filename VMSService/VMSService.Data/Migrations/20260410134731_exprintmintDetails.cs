using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class exprintmintDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BoxNumberPrefix",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "BoxNumberRangeEnd",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "BoxNumberRangeStart",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "NumberOfBoxes",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "NumberOfPieces",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "SerialNumberPrefix",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "SerialNumberRangeEnd",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "SerialNumberRangeStart",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "ExPrintMints");

            migrationBuilder.CreateTable(
                name: "ExPrintMintDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExPrintMintId = table.Column<int>(type: "int", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    NumberOfPieces = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NumberOfBoxes = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExPrintMintDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExPrintMintDetails_ExPrintMints_ExPrintMintId",
                        column: x => x.ExPrintMintId,
                        principalTable: "ExPrintMints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMintDetails_ExPrintMintId",
                table: "ExPrintMintDetails",
                column: "ExPrintMintId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExPrintMintDetails");

            migrationBuilder.AddColumn<string>(
                name: "BoxNumberPrefix",
                table: "ExPrintMints",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "BoxNumberRangeEnd",
                table: "ExPrintMints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BoxNumberRangeStart",
                table: "ExPrintMints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfBoxes",
                table: "ExPrintMints",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfPieces",
                table: "ExPrintMints",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumberPrefix",
                table: "ExPrintMints",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SerialNumberRangeEnd",
                table: "ExPrintMints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SerialNumberRangeStart",
                table: "ExPrintMints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Value",
                table: "ExPrintMints",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
