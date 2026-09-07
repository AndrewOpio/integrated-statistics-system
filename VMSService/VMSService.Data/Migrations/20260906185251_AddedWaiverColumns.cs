using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedWaiverColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "toBeWaived",
                table: "AppliedPenalties",
                newName: "ToBeWaived");

            migrationBuilder.AddColumn<string>(
                name: "AttachmentIds",
                table: "WithdrawalRequestStatusHistory",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RtgsCode",
                table: "WithdrawalRequests",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FundsNotIssued",
                table: "WithdrawalRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ReturnRtgsPending",
                table: "WithdrawalRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RtgsCodeAtLastReturn",
                table: "WithdrawalRequests",
                type: "nvarchar(max)",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "UQ_WithdrawalRequest_RtgsCode_Live",
                table: "WithdrawalRequests",
                column: "RtgsCode",
                unique: true,
                filter: "[RtgsCode] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_WithdrawalRequest_RtgsCode_Live",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "AttachmentIds",
                table: "WithdrawalRequestStatusHistory");

            migrationBuilder.DropColumn(
                name: "FundsNotIssued",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "ReturnRtgsPending",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "RtgsCodeAtLastReturn",
                table: "WithdrawalRequests");

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

            migrationBuilder.AlterColumn<string>(
                name: "RtgsCode",
                table: "WithdrawalRequests",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
