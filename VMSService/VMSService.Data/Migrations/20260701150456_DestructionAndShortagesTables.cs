using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class DestructionAndShortagesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClearingFeesCurrency",
                table: "ReceivedStock",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HandlingChargesCurrency",
                table: "ReceivedStock",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InsuranceFeesCurrency",
                table: "ReceivedStock",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShippingFeesCurrency",
                table: "ReceivedStock",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TestingFeesCurrency",
                table: "ReceivedStock",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BoardSessionSurplusShortages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepositRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommercialBankId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    SessionSourceBranch = table.Column<int>(type: "int", nullable: true),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsSurplus = table.Column<bool>(type: "bit", nullable: false),
                    IsDifferentDenomination = table.Column<bool>(type: "bit", nullable: false),
                    IsProcessedOnRtgs = table.Column<bool>(type: "bit", nullable: false),
                    RtgsCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardSessionSurplusShortages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardSessionSurplusShortages_DepositRequests_DepositRequestId",
                        column: x => x.DepositRequestId,
                        principalTable: "DepositRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DestructionAuthorizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestructionAuthorizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DestructionLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestructionLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DestructionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestructionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EodDiscrepancyReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SessionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EodDiscrepancyReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyCommercialBankSettlements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommercialBankId = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalSurplus = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalShortage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetPosition = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyCommercialBankSettlements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyShortages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Period = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalSurplus = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalShortage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetShortage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyShortages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonthlySurpluses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DepositDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CommercialBankId = table.Column<int>(type: "int", nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlySurpluses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardSessionDifferentDenoms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoardSessionSurplusShortageId = table.Column<int>(type: "int", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardSessionDifferentDenoms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardSessionDifferentDenoms_BoardSessionSurplusShortages_BoardSessionSurplusShortageId",
                        column: x => x.BoardSessionSurplusShortageId,
                        principalTable: "BoardSessionSurplusShortages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DestructionAuthorizationDenominations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DestructionAuthorizationId = table.Column<int>(type: "int", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    NumberOfBags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestructionAuthorizationDenominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestructionAuthorizationDenominations_DestructionAuthorizations_DestructionAuthorizationId",
                        column: x => x.DestructionAuthorizationId,
                        principalTable: "DestructionAuthorizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DestructionAuthorizationStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DestructionAuthorizationId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_DestructionAuthorizationStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestructionAuthorizationStatusHistory_DestructionAuthorizations_DestructionAuthorizationId",
                        column: x => x.DestructionAuthorizationId,
                        principalTable: "DestructionAuthorizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DestructionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DestructionAuthorizationId = table.Column<int>(type: "int", nullable: true),
                    DestructionTypeId = table.Column<int>(type: "int", nullable: false),
                    DestructionLocationId = table.Column<int>(type: "int", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerificationOfficer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestructionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestructionRequests_DestructionAuthorizations_DestructionAuthorizationId",
                        column: x => x.DestructionAuthorizationId,
                        principalTable: "DestructionAuthorizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DestructionRequests_DestructionLocations_DestructionLocationId",
                        column: x => x.DestructionLocationId,
                        principalTable: "DestructionLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DestructionRequests_DestructionTypes_DestructionTypeId",
                        column: x => x.DestructionTypeId,
                        principalTable: "DestructionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EodDiscrepancyReportEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EodDiscrepancyReportId = table.Column<int>(type: "int", nullable: false),
                    BoardSessionSurplusShortageId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EodDiscrepancyReportEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EodDiscrepancyReportEntries_BoardSessionSurplusShortages_BoardSessionSurplusShortageId",
                        column: x => x.BoardSessionSurplusShortageId,
                        principalTable: "BoardSessionSurplusShortages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EodDiscrepancyReportEntries_EodDiscrepancyReports_EodDiscrepancyReportId",
                        column: x => x.EodDiscrepancyReportId,
                        principalTable: "EodDiscrepancyReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EodDiscrepancyReportStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EodDiscrepancyReportId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EodDiscrepancyReportStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EodDiscrepancyReportStatusHistories_EodDiscrepancyReports_EodDiscrepancyReportId",
                        column: x => x.EodDiscrepancyReportId,
                        principalTable: "EodDiscrepancyReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyCommercialBankSettlementStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthlyCommercialBankSettlementId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_MonthlyCommercialBankSettlementStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyCommercialBankSettlementStatusHistories_MonthlyCommercialBankSettlements_MonthlyCommercialBankSettlementId",
                        column: x => x.MonthlyCommercialBankSettlementId,
                        principalTable: "MonthlyCommercialBankSettlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyShortageDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthlyShortageId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    TotalSurplus = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalShortage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetShortage = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyShortageDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyShortageDetails_MonthlyShortages_MonthlyShortageId",
                        column: x => x.MonthlyShortageId,
                        principalTable: "MonthlyShortages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyShortageStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthlyShortageId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_MonthlyShortageStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyShortageStatusHistories_MonthlyShortages_MonthlyShortageId",
                        column: x => x.MonthlyShortageId,
                        principalTable: "MonthlyShortages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonthlySurplusStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthlySurplusId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_MonthlySurplusStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlySurplusStatusHistories_MonthlySurpluses_MonthlySurplusId",
                        column: x => x.MonthlySurplusId,
                        principalTable: "MonthlySurpluses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DestructionRequestDenominations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DestructionRequestId = table.Column<int>(type: "int", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    NumberOfBags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberOfNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Destroyed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestructionRequestDenominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestructionRequestDenominations_DestructionRequests_DestructionRequestId",
                        column: x => x.DestructionRequestId,
                        principalTable: "DestructionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DestructionRequestMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DestructionRequestId = table.Column<int>(type: "int", nullable: false),
                    IDNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsWitness = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestructionRequestMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestructionRequestMembers_DestructionRequests_DestructionRequestId",
                        column: x => x.DestructionRequestId,
                        principalTable: "DestructionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DestructionRequestStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DestructionRequestId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_DestructionRequestStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestructionRequestStatusHistory_DestructionRequests_DestructionRequestId",
                        column: x => x.DestructionRequestId,
                        principalTable: "DestructionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardSessionDifferentDenoms_BoardSessionSurplusShortageId",
                table: "BoardSessionDifferentDenoms",
                column: "BoardSessionSurplusShortageId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSessionSurplusShortages_DepositRequestId",
                table: "BoardSessionSurplusShortages",
                column: "DepositRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DestructionAuthorizationDenominations_DestructionAuthorizationId",
                table: "DestructionAuthorizationDenominations",
                column: "DestructionAuthorizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DestructionAuthorizationStatusHistory_DestructionAuthorizationId",
                table: "DestructionAuthorizationStatusHistory",
                column: "DestructionAuthorizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DestructionRequestDenominations_DestructionRequestId",
                table: "DestructionRequestDenominations",
                column: "DestructionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DestructionRequestMembers_DestructionRequestId",
                table: "DestructionRequestMembers",
                column: "DestructionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DestructionRequests_DestructionAuthorizationId",
                table: "DestructionRequests",
                column: "DestructionAuthorizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DestructionRequests_DestructionLocationId",
                table: "DestructionRequests",
                column: "DestructionLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_DestructionRequests_DestructionTypeId",
                table: "DestructionRequests",
                column: "DestructionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DestructionRequestStatusHistory_DestructionRequestId",
                table: "DestructionRequestStatusHistory",
                column: "DestructionRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_EodDiscrepancyReportEntries_BoardSessionSurplusShortageId",
                table: "EodDiscrepancyReportEntries",
                column: "BoardSessionSurplusShortageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EodDiscrepancyReportEntries_EodDiscrepancyReportId",
                table: "EodDiscrepancyReportEntries",
                column: "EodDiscrepancyReportId");

            migrationBuilder.CreateIndex(
                name: "IX_EodDiscrepancyReportStatusHistories_EodDiscrepancyReportId",
                table: "EodDiscrepancyReportStatusHistories",
                column: "EodDiscrepancyReportId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyCommercialBankSettlementStatusHistories_MonthlyCommercialBankSettlementId",
                table: "MonthlyCommercialBankSettlementStatusHistories",
                column: "MonthlyCommercialBankSettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyShortageDetails_MonthlyShortageId",
                table: "MonthlyShortageDetails",
                column: "MonthlyShortageId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyShortageStatusHistories_MonthlyShortageId",
                table: "MonthlyShortageStatusHistories",
                column: "MonthlyShortageId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlySurplusStatusHistories_MonthlySurplusId",
                table: "MonthlySurplusStatusHistories",
                column: "MonthlySurplusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardSessionDifferentDenoms");

            migrationBuilder.DropTable(
                name: "DestructionAuthorizationDenominations");

            migrationBuilder.DropTable(
                name: "DestructionAuthorizationStatusHistory");

            migrationBuilder.DropTable(
                name: "DestructionRequestDenominations");

            migrationBuilder.DropTable(
                name: "DestructionRequestMembers");

            migrationBuilder.DropTable(
                name: "DestructionRequestStatusHistory");

            migrationBuilder.DropTable(
                name: "EodDiscrepancyReportEntries");

            migrationBuilder.DropTable(
                name: "EodDiscrepancyReportStatusHistories");

            migrationBuilder.DropTable(
                name: "MonthlyCommercialBankSettlementStatusHistories");

            migrationBuilder.DropTable(
                name: "MonthlyShortageDetails");

            migrationBuilder.DropTable(
                name: "MonthlyShortageStatusHistories");

            migrationBuilder.DropTable(
                name: "MonthlySurplusStatusHistories");

            migrationBuilder.DropTable(
                name: "DestructionRequests");

            migrationBuilder.DropTable(
                name: "BoardSessionSurplusShortages");

            migrationBuilder.DropTable(
                name: "EodDiscrepancyReports");

            migrationBuilder.DropTable(
                name: "MonthlyCommercialBankSettlements");

            migrationBuilder.DropTable(
                name: "MonthlyShortages");

            migrationBuilder.DropTable(
                name: "MonthlySurpluses");

            migrationBuilder.DropTable(
                name: "DestructionAuthorizations");

            migrationBuilder.DropTable(
                name: "DestructionLocations");

            migrationBuilder.DropTable(
                name: "DestructionTypes");

            migrationBuilder.DropColumn(
                name: "ClearingFeesCurrency",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "HandlingChargesCurrency",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "InsuranceFeesCurrency",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "ShippingFeesCurrency",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "TestingFeesCurrency",
                table: "ReceivedStock");
        }
    }
}
