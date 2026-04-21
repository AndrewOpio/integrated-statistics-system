using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBundleTypeAndDenominationBoxes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BundleType",
                table: "WithdrawalRequestDenominations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "WithdrawalRequestDenominationBoxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WithdrawalRequestDenominationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoxNo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithdrawalRequestDenominationBoxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WithdrawalRequestDenominationBoxes_WithdrawalRequestDenominations_WithdrawalRequestDenominationId",
                        column: x => x.WithdrawalRequestDenominationId,
                        principalTable: "WithdrawalRequestDenominations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequestDenominationBoxes_BoxNo",
                table: "WithdrawalRequestDenominationBoxes",
                column: "BoxNo");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequestDenominationBoxes_WithdrawalRequestDenominationId",
                table: "WithdrawalRequestDenominationBoxes",
                column: "WithdrawalRequestDenominationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WithdrawalRequestDenominationBoxes");

            migrationBuilder.DropColumn(
                name: "BundleType",
                table: "WithdrawalRequestDenominations");
        }
    }
}
