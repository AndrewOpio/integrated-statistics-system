using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameToBeWaivedAndAddWaiverFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "toBeWaived",
                table: "AppliedPenalties",
                newName: "ToBeWaived");

            migrationBuilder.AddColumn<string>(
                name: "WaivedBy",
                table: "AppliedPenalties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WaivedDate",
                table: "AppliedPenalties",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WaiverReason",
                table: "AppliedPenalties",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WaivedBy",
                table: "AppliedPenalties");

            migrationBuilder.DropColumn(
                name: "WaivedDate",
                table: "AppliedPenalties");

            migrationBuilder.DropColumn(
                name: "WaiverReason",
                table: "AppliedPenalties");

            migrationBuilder.RenameColumn(
                name: "ToBeWaived",
                table: "AppliedPenalties",
                newName: "toBeWaived");
        }
    }
}
