using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedSessionBag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockProcessingSessions_DepositRequestDenominations_DepositRequestDenominationId",
                table: "StockProcessingSessions");

            migrationBuilder.DropIndex(
                name: "IX_StockProcessingSessions_DepositRequestDenominationId",
                table: "StockProcessingSessions");

            migrationBuilder.DropColumn(
                name: "DepositRequestDenominationId",
                table: "StockProcessingSessions");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Bags",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubCategoryId",
                table: "Bags",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Bags");

            migrationBuilder.DropColumn(
                name: "SubCategoryId",
                table: "Bags");

            migrationBuilder.AddColumn<Guid>(
                name: "DepositRequestDenominationId",
                table: "StockProcessingSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockProcessingSessions_DepositRequestDenominationId",
                table: "StockProcessingSessions",
                column: "DepositRequestDenominationId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockProcessingSessions_DepositRequestDenominations_DepositRequestDenominationId",
                table: "StockProcessingSessions",
                column: "DepositRequestDenominationId",
                principalTable: "DepositRequestDenominations",
                principalColumn: "Id");
        }
    }
}
