using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedBranchIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovedCurrencyDenominations_Denominations_DenominationId",
                table: "ApprovedCurrencyDenominations");

            migrationBuilder.DropForeignKey(
                name: "FK_ExPrintMints_Denominations_DenominationId",
                table: "ExPrintMints");

            migrationBuilder.DropTable(
                name: "Denominations");

            migrationBuilder.DropIndex(
                name: "IX_ExPrintMints_DenominationId",
                table: "ExPrintMints");

            migrationBuilder.DropIndex(
                name: "IX_ApprovedCurrencyDenominations_DenominationId",
                table: "ApprovedCurrencyDenominations");

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "ExPrintMints",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ExPrintMints");

            migrationBuilder.CreateTable(
                name: "Denominations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DenominationTypeId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumberOfPiecesInABox = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Denominations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMints_DenominationId",
                table: "ExPrintMints",
                column: "DenominationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedCurrencyDenominations_DenominationId",
                table: "ApprovedCurrencyDenominations",
                column: "DenominationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovedCurrencyDenominations_Denominations_DenominationId",
                table: "ApprovedCurrencyDenominations",
                column: "DenominationId",
                principalTable: "Denominations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExPrintMints_Denominations_DenominationId",
                table: "ExPrintMints",
                column: "DenominationId",
                principalTable: "Denominations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
