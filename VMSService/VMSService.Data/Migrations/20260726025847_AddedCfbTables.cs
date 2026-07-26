using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedCfbTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReturnRtgsCode",
                table: "WithdrawalRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ReturnRtgsDate",
                table: "WithdrawalRequests",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CfbBoxes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BouBranchId = table.Column<int>(type: "int", nullable: false),
                    BouBranchCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BoxType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CfbBoxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Penalties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppliesTo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    CutOffTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    PenaltyValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PenaltyPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PercentCutOff = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    PenaltyMultiplier = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Penalties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TopupRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BouBranchId = table.Column<int>(type: "int", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopupRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CfbCashHoldings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CfbBoxId = table.Column<int>(type: "int", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    DenomValue = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CfbCashHoldings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CfbCashHoldings_CfbBoxes_CfbBoxId",
                        column: x => x.CfbBoxId,
                        principalTable: "CfbBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CfbCashMovements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CfbBoxId = table.Column<int>(type: "int", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    DenomValue = table.Column<int>(type: "int", nullable: false),
                    AmountChange = table.Column<long>(type: "bigint", nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SourceId = table.Column<int>(type: "int", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CfbCashMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CfbCashMovements_CfbBoxes_CfbBoxId",
                        column: x => x.CfbBoxId,
                        principalTable: "CfbBoxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppliedPenalties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PenaltyId = table.Column<int>(type: "int", nullable: false),
                    ChargeValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DenomValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    IsSettledOnRtgs = table.Column<bool>(type: "bit", nullable: false),
                    RtgsCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RtgsSettlingMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppliedPenalties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppliedPenalties_Penalties_PenaltyId",
                        column: x => x.PenaltyId,
                        principalTable: "Penalties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TopupLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TopupRequestId = table.Column<int>(type: "int", nullable: false),
                    CfbBoxId = table.Column<int>(type: "int", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    DenomValue = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    BaselineBeforeTopup = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopupLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopupLines_TopupRequests_TopupRequestId",
                        column: x => x.TopupRequestId,
                        principalTable: "TopupRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TopupRequestStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TopupRequestId = table.Column<int>(type: "int", nullable: false),
                    OldStatusId = table.Column<int>(type: "int", nullable: false),
                    OldStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewStatusId = table.Column<int>(type: "int", nullable: false),
                    NewStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopupRequestStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopupRequestStatusHistories_TopupRequests_TopupRequestId",
                        column: x => x.TopupRequestId,
                        principalTable: "TopupRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppliedPenalties_PenaltyId",
                table: "AppliedPenalties",
                column: "PenaltyId");

            migrationBuilder.CreateIndex(
                name: "IX_AppliedPenalties_SourceId_PenaltyId",
                table: "AppliedPenalties",
                columns: new[] { "SourceId", "PenaltyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CfbBoxes_BouBranchId_BoxType",
                table: "CfbBoxes",
                columns: new[] { "BouBranchId", "BoxType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CfbCashHoldings_Active_Box_Denom",
                table: "CfbCashHoldings",
                columns: new[] { "CfbBoxId", "DenominationId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CfbCashMovements_CfbBoxId_DenominationId_OccurredAt",
                table: "CfbCashMovements",
                columns: new[] { "CfbBoxId", "DenominationId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Penalties_Name",
                table: "Penalties",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TopupLines_TopupRequestId",
                table: "TopupLines",
                column: "TopupRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TopupRequests_BouBranchId",
                table: "TopupRequests",
                column: "BouBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_TopupRequests_CurrentStatusId",
                table: "TopupRequests",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_TopupRequestStatusHistories_TopupRequestId",
                table: "TopupRequestStatusHistories",
                column: "TopupRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppliedPenalties");

            migrationBuilder.DropTable(
                name: "CfbCashHoldings");

            migrationBuilder.DropTable(
                name: "CfbCashMovements");

            migrationBuilder.DropTable(
                name: "TopupLines");

            migrationBuilder.DropTable(
                name: "TopupRequestStatusHistories");

            migrationBuilder.DropTable(
                name: "Penalties");

            migrationBuilder.DropTable(
                name: "CfbBoxes");

            migrationBuilder.DropTable(
                name: "TopupRequests");

            migrationBuilder.DropColumn(
                name: "ReturnRtgsCode",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "ReturnRtgsDate",
                table: "WithdrawalRequests");
        }
    }
}
