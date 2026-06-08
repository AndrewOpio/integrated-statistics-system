using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class BaggingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bags_DepositRequestDenominations_DepositRequestDenominationId",
                table: "Bags");

            migrationBuilder.DropIndex(
                name: "IX_Bags_DepositRequestDenominationId",
                table: "Bags");

            migrationBuilder.DropColumn(
                name: "DepositRequestDenominationId",
                table: "Bags");

            migrationBuilder.AddColumn<int>(
                name: "DenominationId",
                table: "Bags",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BagDenominations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BagId = table.Column<int>(type: "int", nullable: false),
                    DepositRequestDenominationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BagDenominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BagDenominations_Bags_BagId",
                        column: x => x.BagId,
                        principalTable: "Bags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BagDenominations_DepositRequestDenominations_DepositRequestDenominationId",
                        column: x => x.DepositRequestDenominationId,
                        principalTable: "DepositRequestDenominations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BagDenominations_BagId",
                table: "BagDenominations",
                column: "BagId");

            migrationBuilder.CreateIndex(
                name: "IX_BagDenominations_DepositRequestDenominationId",
                table: "BagDenominations",
                column: "DepositRequestDenominationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BagDenominations");

            migrationBuilder.DropColumn(
                name: "DenominationId",
                table: "Bags");

            migrationBuilder.AddColumn<Guid>(
                name: "DepositRequestDenominationId",
                table: "Bags",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Bags_DepositRequestDenominationId",
                table: "Bags",
                column: "DepositRequestDenominationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bags_DepositRequestDenominations_DepositRequestDenominationId",
                table: "Bags",
                column: "DepositRequestDenominationId",
                principalTable: "DepositRequestDenominations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
