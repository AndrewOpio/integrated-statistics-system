using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProcessingPenaltiesAndStockTakeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BagMovements_Bags_BagId",
                table: "BagMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_BagMovements_VaultSections_OldVaultSectionId",
                table: "BagMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardSessionDifferentDenoms_BoardSessionSurplusShortages_BoardSessionSurplusShortageId",
                table: "BoardSessionDifferentDenoms");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardSessionSurplusShortages_DepositRequests_DepositRequestId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropForeignKey(
                name: "FK_StockProcessingPlans_StockMovements_StockMovementId",
                table: "StockProcessingPlans");

            migrationBuilder.DropIndex(
                name: "IX_StockProcessingPlans_StockMovementId",
                table: "StockProcessingPlans");

            migrationBuilder.DropColumn(
                name: "StockMovementId",
                table: "StockProcessingPlans");

            migrationBuilder.RenameColumn(
                name: "UnfitShredValue",
                table: "StockProcessingResults",
                newName: "UnFitShredValue");

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

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "StockProcessingSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalIssuedValue",
                table: "StockProcessingSessions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "StockProcessingResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "UnProcessedValue",
                table: "StockProcessingResults",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "StockProcessingPlans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsLegacyDeposit",
                table: "DepositRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CfbBoxId",
                table: "BoardSessionSurplusShortages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStatusId",
                table: "BoardSessionSurplusShortages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "BoardSessionSurplusShortages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StockProcessingSessionId",
                table: "BoardSessionSurplusShortages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "BoardSessionSurplusShortageId",
                table: "BoardSessionDifferentDenoms",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "NumberOfPieces",
                table: "BoardSessionDifferentDenoms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StockProcessingSessionId",
                table: "BoardSessionDifferentDenoms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsLegacyBag",
                table: "Bags",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "OldVaultSectionId",
                table: "BagMovements",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "BagId",
                table: "BagMovements",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "BagMovementBags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BagMovementId = table.Column<int>(type: "int", nullable: false),
                    BagId = table.Column<int>(type: "int", nullable: false),
                    OldVaultSectionId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BagMovementBags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BagMovementBags_BagMovements_BagMovementId",
                        column: x => x.BagMovementId,
                        principalTable: "BagMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BagMovementBags_Bags_BagId",
                        column: x => x.BagId,
                        principalTable: "Bags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BagMovementBags_VaultSections_OldVaultSectionId",
                        column: x => x.OldVaultSectionId,
                        principalTable: "VaultSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BoardSessionSurplusShortageStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoardSessionSurplusShortageId = table.Column<int>(type: "int", nullable: false),
                    OldStatusId = table.Column<int>(type: "int", nullable: false),
                    NewStatusId = table.Column<int>(type: "int", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardSessionSurplusShortageStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardSessionSurplusShortageStatusHistory_BoardSessionSurplusShortages_BoardSessionSurplusShortageId",
                        column: x => x.BoardSessionSurplusShortageId,
                        principalTable: "BoardSessionSurplusShortages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "MIandFits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockProcessingPlanId = table.Column<int>(type: "int", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    IssuedFitValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IssuedUnFitValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReturnedFitValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReturnedUnFitValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BoardSupervisorUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MIandFits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MIandFits_StockProcessingPlans_StockProcessingPlanId",
                        column: x => x.StockProcessingPlanId,
                        principalTable: "StockProcessingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NoteExaminers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteExaminers", x => x.Id);
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
                name: "PublicHolidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicHolidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockTakes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CountDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    AmendsStockTakeId = table.Column<int>(type: "int", nullable: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTakes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTakes_StockTakes_AmendsStockTakeId",
                        column: x => x.AmendsStockTakeId,
                        principalTable: "StockTakes",
                        principalColumn: "Id");
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
                name: "StockProcessingSessionNoteExaminers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockProcessingSessionId = table.Column<int>(type: "int", nullable: false),
                    NoteExaminerId = table.Column<int>(type: "int", nullable: false),
                    NumberOfBundles = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockProcessingSessionNoteExaminers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockProcessingSessionNoteExaminers_NoteExaminers_NoteExaminerId",
                        column: x => x.NoteExaminerId,
                        principalTable: "NoteExaminers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockProcessingSessionNoteExaminers_StockProcessingSessions_StockProcessingSessionId",
                        column: x => x.StockProcessingSessionId,
                        principalTable: "StockProcessingSessions",
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
                    toBeWaived = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
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
                name: "PenaltyWaivers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PenaltyId = table.Column<int>(type: "int", nullable: false),
                    BouBranchId = table.Column<int>(type: "int", nullable: false),
                    BouBranchCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PenaltyWaivers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PenaltyWaivers_Penalties_PenaltyId",
                        column: x => x.PenaltyId,
                        principalTable: "Penalties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockTakeDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockTakeId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    VaultSectionId = table.Column<int>(type: "int", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    NumberOfPieces = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTakeDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTakeDetails_StockTakes_StockTakeId",
                        column: x => x.StockTakeId,
                        principalTable: "StockTakes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockTakeDetails_VaultSections_VaultSectionId",
                        column: x => x.VaultSectionId,
                        principalTable: "VaultSections",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockTakeStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockTakeId = table.Column<int>(type: "int", nullable: false),
                    OldStatusId = table.Column<int>(type: "int", nullable: false),
                    OldStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewStatusId = table.Column<int>(type: "int", nullable: false),
                    NewStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTakeStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTakeStatusHistory_StockTakes_StockTakeId",
                        column: x => x.StockTakeId,
                        principalTable: "StockTakes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockTakeSystemPositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockTakeId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTakeSystemPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTakeSystemPositions_StockTakes_StockTakeId",
                        column: x => x.StockTakeId,
                        principalTable: "StockTakes",
                        principalColumn: "Id");
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
                name: "IX_BoardSessionSurplusShortages_CfbBoxId",
                table: "BoardSessionSurplusShortages",
                column: "CfbBoxId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSessionSurplusShortages_StockProcessingSessionId",
                table: "BoardSessionSurplusShortages",
                column: "StockProcessingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSessionDifferentDenoms_StockProcessingSessionId",
                table: "BoardSessionDifferentDenoms",
                column: "StockProcessingSessionId");

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
                name: "IX_BagMovementBags_BagId",
                table: "BagMovementBags",
                column: "BagId");

            migrationBuilder.CreateIndex(
                name: "IX_BagMovementBags_BagMovementId",
                table: "BagMovementBags",
                column: "BagMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_BagMovementBags_OldVaultSectionId",
                table: "BagMovementBags",
                column: "OldVaultSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSessionSurplusShortageStatusHistory_BoardSessionSurplusShortageId",
                table: "BoardSessionSurplusShortageStatusHistory",
                column: "BoardSessionSurplusShortageId");

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
                name: "IX_MIandFits_StockProcessingPlanId",
                table: "MIandFits",
                column: "StockProcessingPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Penalties_Name",
                table: "Penalties",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PenaltyWaivers_PenaltyId",
                table: "PenaltyWaivers",
                column: "PenaltyId");

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidays_Date",
                table: "PublicHolidays",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockProcessingSessionNoteExaminers_NoteExaminerId",
                table: "StockProcessingSessionNoteExaminers",
                column: "NoteExaminerId");

            migrationBuilder.CreateIndex(
                name: "IX_StockProcessingSessionNoteExaminers_StockProcessingSessionId",
                table: "StockProcessingSessionNoteExaminers",
                column: "StockProcessingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakeDetails_StockTakeId",
                table: "StockTakeDetails",
                column: "StockTakeId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakeDetails_VaultSectionId",
                table: "StockTakeDetails",
                column: "VaultSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakes_AmendsStockTakeId",
                table: "StockTakes",
                column: "AmendsStockTakeId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakeStatusHistory_StockTakeId",
                table: "StockTakeStatusHistory",
                column: "StockTakeId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTakeSystemPositions_StockTakeId",
                table: "StockTakeSystemPositions",
                column: "StockTakeId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_BagMovements_Bags_BagId",
                table: "BagMovements",
                column: "BagId",
                principalTable: "Bags",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BagMovements_VaultSections_OldVaultSectionId",
                table: "BagMovements",
                column: "OldVaultSectionId",
                principalTable: "VaultSections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardSessionDifferentDenoms_BoardSessionSurplusShortages_BoardSessionSurplusShortageId",
                table: "BoardSessionDifferentDenoms",
                column: "BoardSessionSurplusShortageId",
                principalTable: "BoardSessionSurplusShortages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardSessionDifferentDenoms_StockProcessingSessions_StockProcessingSessionId",
                table: "BoardSessionDifferentDenoms",
                column: "StockProcessingSessionId",
                principalTable: "StockProcessingSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardSessionSurplusShortages_CfbBoxes_CfbBoxId",
                table: "BoardSessionSurplusShortages",
                column: "CfbBoxId",
                principalTable: "CfbBoxes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardSessionSurplusShortages_DepositRequests_DepositRequestId",
                table: "BoardSessionSurplusShortages",
                column: "DepositRequestId",
                principalTable: "DepositRequests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardSessionSurplusShortages_StockProcessingSessions_StockProcessingSessionId",
                table: "BoardSessionSurplusShortages",
                column: "StockProcessingSessionId",
                principalTable: "StockProcessingSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BagMovements_Bags_BagId",
                table: "BagMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_BagMovements_VaultSections_OldVaultSectionId",
                table: "BagMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardSessionDifferentDenoms_BoardSessionSurplusShortages_BoardSessionSurplusShortageId",
                table: "BoardSessionDifferentDenoms");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardSessionDifferentDenoms_StockProcessingSessions_StockProcessingSessionId",
                table: "BoardSessionDifferentDenoms");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardSessionSurplusShortages_CfbBoxes_CfbBoxId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardSessionSurplusShortages_DepositRequests_DepositRequestId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardSessionSurplusShortages_StockProcessingSessions_StockProcessingSessionId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropTable(
                name: "AppliedPenalties");

            migrationBuilder.DropTable(
                name: "BagMovementBags");

            migrationBuilder.DropTable(
                name: "BoardSessionSurplusShortageStatusHistory");

            migrationBuilder.DropTable(
                name: "CfbCashHoldings");

            migrationBuilder.DropTable(
                name: "CfbCashMovements");

            migrationBuilder.DropTable(
                name: "MIandFits");

            migrationBuilder.DropTable(
                name: "PenaltyWaivers");

            migrationBuilder.DropTable(
                name: "PublicHolidays");

            migrationBuilder.DropTable(
                name: "StockProcessingSessionNoteExaminers");

            migrationBuilder.DropTable(
                name: "StockTakeDetails");

            migrationBuilder.DropTable(
                name: "StockTakeStatusHistory");

            migrationBuilder.DropTable(
                name: "StockTakeSystemPositions");

            migrationBuilder.DropTable(
                name: "TopupLines");

            migrationBuilder.DropTable(
                name: "TopupRequestStatusHistories");

            migrationBuilder.DropTable(
                name: "CfbBoxes");

            migrationBuilder.DropTable(
                name: "Penalties");

            migrationBuilder.DropTable(
                name: "NoteExaminers");

            migrationBuilder.DropTable(
                name: "StockTakes");

            migrationBuilder.DropTable(
                name: "TopupRequests");

            migrationBuilder.DropIndex(
                name: "IX_BoardSessionSurplusShortages_CfbBoxId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropIndex(
                name: "IX_BoardSessionSurplusShortages_StockProcessingSessionId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropIndex(
                name: "IX_BoardSessionDifferentDenoms_StockProcessingSessionId",
                table: "BoardSessionDifferentDenoms");

            migrationBuilder.DropColumn(
                name: "ReturnRtgsCode",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "ReturnRtgsDate",
                table: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "StockProcessingSessions");

            migrationBuilder.DropColumn(
                name: "TotalIssuedValue",
                table: "StockProcessingSessions");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "StockProcessingResults");

            migrationBuilder.DropColumn(
                name: "UnProcessedValue",
                table: "StockProcessingResults");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "StockProcessingPlans");

            migrationBuilder.DropColumn(
                name: "IsLegacyDeposit",
                table: "DepositRequests");

            migrationBuilder.DropColumn(
                name: "CfbBoxId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropColumn(
                name: "CurrentStatusId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropColumn(
                name: "StockProcessingSessionId",
                table: "BoardSessionSurplusShortages");

            migrationBuilder.DropColumn(
                name: "NumberOfPieces",
                table: "BoardSessionDifferentDenoms");

            migrationBuilder.DropColumn(
                name: "StockProcessingSessionId",
                table: "BoardSessionDifferentDenoms");

            migrationBuilder.DropColumn(
                name: "IsLegacyBag",
                table: "Bags");

            migrationBuilder.RenameColumn(
                name: "UnFitShredValue",
                table: "StockProcessingResults",
                newName: "UnfitShredValue");

            migrationBuilder.AddColumn<int>(
                name: "StockMovementId",
                table: "StockProcessingPlans",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BoardSessionSurplusShortageId",
                table: "BoardSessionDifferentDenoms",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OldVaultSectionId",
                table: "BagMovements",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BagId",
                table: "BagMovements",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockProcessingPlans_StockMovementId",
                table: "StockProcessingPlans",
                column: "StockMovementId");

            migrationBuilder.AddForeignKey(
                name: "FK_BagMovements_Bags_BagId",
                table: "BagMovements",
                column: "BagId",
                principalTable: "Bags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BagMovements_VaultSections_OldVaultSectionId",
                table: "BagMovements",
                column: "OldVaultSectionId",
                principalTable: "VaultSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardSessionDifferentDenoms_BoardSessionSurplusShortages_BoardSessionSurplusShortageId",
                table: "BoardSessionDifferentDenoms",
                column: "BoardSessionSurplusShortageId",
                principalTable: "BoardSessionSurplusShortages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardSessionSurplusShortages_DepositRequests_DepositRequestId",
                table: "BoardSessionSurplusShortages",
                column: "DepositRequestId",
                principalTable: "DepositRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StockProcessingPlans_StockMovements_StockMovementId",
                table: "StockProcessingPlans",
                column: "StockMovementId",
                principalTable: "StockMovements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
