using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RTGSChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RtgsDate",
                table: "WithdrawalRequests",
                newName: "RtgsVerifiedDate");

            migrationBuilder.AddColumn<DateOnly>(
                name: "FundSettlementValueDate",
                table: "WithdrawalRequests",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecallRtgsCode",
                table: "WithdrawalRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RecallRtgsDate",
                table: "WithdrawalRequests",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RecallRtgsPending",
                table: "WithdrawalRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RtgsVerificationStatus",
                table: "WithdrawalRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BundleType",
                table: "WithdrawalRequestDenominations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BoxStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackingListDetailsId = table.Column<long>(type: "bigint", nullable: false),
                    CaseNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    BranchIsAtId = table.Column<int>(type: "int", nullable: false),
                    CurrentSerialCursor = table.Column<long>(type: "bigint", nullable: false),
                    RemainingBundles = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    LastDispatchedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastDispatchedRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoxStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoxStates_PackingListDetails_PackingListDetailsId",
                        column: x => x.PackingListDetailsId,
                        principalTable: "PackingListDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WithdrawalRequestDenominationBoxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WithdrawalRequestDenominationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BundlesTaken = table.Column<int>(type: "int", nullable: false),
                    SerialStartTaken = table.Column<long>(type: "bigint", nullable: false),
                    SerialEndTaken = table.Column<long>(type: "bigint", nullable: false),
                    SerialPrefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                name: "IX_BoxStates_PackingListDetailsId",
                table: "BoxStates",
                column: "PackingListDetailsId");

            migrationBuilder.CreateIndex(
                name: "UQ_BoxState_Box_Denom_Branch",
                table: "BoxStates",
                columns: new[] { "CaseNumber", "DenominationId", "BranchIsAtId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequestDenominationBoxes_CaseNumber",
                table: "WithdrawalRequestDenominationBoxes",
                column: "CaseNumber");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequestDenominationBoxes_WithdrawalRequestDenominationId",
                table: "WithdrawalRequestDenominationBoxes",
                column: "WithdrawalRequestDenominationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoxStates");

            migrationBuilder.DropTable(
                name: "WithdrawalRequestDenominationBoxes");

            migrationBuilder.DropColumn(
                name: "FundSettlementValueDate",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "RecallRtgsCode",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "RecallRtgsDate",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "RecallRtgsPending",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "RtgsVerificationStatus",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "BundleType",
                table: "WithdrawalRequestDenominations");

            migrationBuilder.RenameColumn(
                name: "RtgsVerifiedDate",
                table: "WithdrawalRequests",
                newName: "RtgsDate");
        }
    }
}
