using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProcessingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentDetailStatusHistory_AgentDetails_AgentDetailId",
                table: "AgentDetailStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovedCurrencyDenominations_ApprovedCurrencyProcurements_ApprovedCurrencyId",
                table: "ApprovedCurrencyDenominations");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovedCurrencyDenominations_CurrencyVendors_CurrencyVendorId",
                table: "ApprovedCurrencyDenominations");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovedCurrencyStatusHistory_ApprovedCurrencyProcurements_ApprovedCurrencyProcurementId",
                table: "ApprovedCurrencyStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_CurrencyVendors_VendorTypes_VendorTypeId",
                table: "CurrencyVendors");

            migrationBuilder.DropForeignKey(
                name: "FK_DepositFees_DepositRequests_DepositRequestId",
                table: "DepositFees");

            migrationBuilder.DropForeignKey(
                name: "FK_DepositFees_FeeTypes_FeeTypeId",
                table: "DepositFees");

            migrationBuilder.DropForeignKey(
                name: "FK_DepositRequestAgents_DepositRequests_DepositRequestId",
                table: "DepositRequestAgents");

            migrationBuilder.DropForeignKey(
                name: "FK_DepositRequestDenominations_DepositRequests_DepositRequestId",
                table: "DepositRequestDenominations");

            migrationBuilder.DropForeignKey(
                name: "FK_DepositRequestStatusHistory_DepositRequests_DepositRequestId",
                table: "DepositRequestStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ExPrintMintDetails_ExPrintMints_ExPrintMintId",
                table: "ExPrintMintDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_ExPrintMints_ApprovedCurrencyProcurements_ApprovedCurrencyProcurementId",
                table: "ExPrintMints");

            migrationBuilder.DropForeignKey(
                name: "FK_ExPrintMints_CurrencyVendors_CurrencyVendorId",
                table: "ExPrintMints");

            migrationBuilder.DropForeignKey(
                name: "FK_ExPrintMintStatusHistory_ExPrintMints_ExPrintMintId",
                table: "ExPrintMintStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_LogisticsCategories_Logistics_LogisticsId",
                table: "LogisticsCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_LogisticsDetails_LogisticsCategories_LogisticsCategoryId",
                table: "LogisticsDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_LogisticsStatusHistory_Logistics_LogisticsId",
                table: "LogisticsStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_NewStockMovementLogs_NewCaseFullStatusTypes_CaseFullStatusId",
                table: "NewStockMovementLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_NewStockMovementLogs_NewStockAccountingStatuses_AccountingStatusId",
                table: "NewStockMovementLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_NewStockMovementLogs_PackingListDetails_PackingListDetailId",
                table: "NewStockMovementLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_NewStockMovementLogs_StockMovements_StockMovementId",
                table: "NewStockMovementLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_PackingListRanges_ExPrintMintDetails_ExPrintMintDetailId",
                table: "PackingListRanges");

            migrationBuilder.DropForeignKey(
                name: "FK_PackingListRanges_ExPrintMints_ExpectedStockId",
                table: "PackingListRanges");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedStock_ExPrintMints_ExPrintMintId",
                table: "ReceivedStock");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedStockStatusHistory_ReceivedStock_ReceivedStockId",
                table: "ReceivedStockStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovementDenominationBoxNumberRanges_StockMovementDenominations_StockMovementDenominationId",
                table: "StockMovementDenominationBoxNumberRanges");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovementDenominations_StockMovements_StockMovementId",
                table: "StockMovementDenominations");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovementDiscrepancies_StockMovements_StockMovementId",
                table: "StockMovementDiscrepancies");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovementStatusHistory_StockMovements_StockMovementId",
                table: "StockMovementStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_WithdrawalRequestAgents_WithdrawalRequests_WithdrawalRequestId",
                table: "WithdrawalRequestAgents");

            migrationBuilder.DropForeignKey(
                name: "FK_WithdrawalRequestDenominations_WithdrawalRequests_WithdrawalRequestId",
                table: "WithdrawalRequestDenominations");

            migrationBuilder.DropForeignKey(
                name: "FK_WithdrawalRequestStatusHistory_WithdrawalRequests_WithdrawalRequestId",
                table: "WithdrawalRequestStatusHistory");

            migrationBuilder.DropColumn(
                name: "NewCaseFullStatus",
                table: "NewStockMovementLogs");

            migrationBuilder.DropColumn(
                name: "NewStockAccountingStatus",
                table: "NewStockMovementLogs");

            migrationBuilder.AlterColumn<int>(
                name: "CaseFullStatusId",
                table: "NewStockMovementLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "AccountingStatusId",
                table: "NewStockMovementLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Attachments",
                table: "DepositRequestStatusHistory",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BPSMachines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BPSMachines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockProcessingPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    StockMovementId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockProcessingPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockProcessingPlans_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcessingBoards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    BPSMachineId = table.Column<int>(type: "int", nullable: true),
                    BoardSupervisorUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssistantSupervisorUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingBoards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessingBoards_BPSMachines_BPSMachineId",
                        column: x => x.BPSMachineId,
                        principalTable: "BPSMachines",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StockProcessingPlanStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockProcessingPlanId = table.Column<int>(type: "int", nullable: false),
                    OldStatusId = table.Column<int>(type: "int", nullable: false),
                    NewStatusId = table.Column<int>(type: "int", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Attachments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockProcessingPlanStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockProcessingPlanStatusHistory_StockProcessingPlans_StockProcessingPlanId",
                        column: x => x.StockProcessingPlanId,
                        principalTable: "StockProcessingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockProcessingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StockProcessingPlanId = table.Column<int>(type: "int", nullable: false),
                    CommercialBankId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    ProcessingBoardId = table.Column<int>(type: "int", nullable: true),
                    DepositRequestDenominationId = table.Column<int>(type: "int", nullable: false),
                    DepositRequestDenominationId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    IsManual = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockProcessingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockProcessingSessions_DepositRequestDenominations_DepositRequestDenominationId1",
                        column: x => x.DepositRequestDenominationId1,
                        principalTable: "DepositRequestDenominations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockProcessingSessions_ProcessingBoards_ProcessingBoardId",
                        column: x => x.ProcessingBoardId,
                        principalTable: "ProcessingBoards",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockProcessingSessions_StockProcessingPlans_StockProcessingPlanId",
                        column: x => x.StockProcessingPlanId,
                        principalTable: "StockProcessingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockProcessingResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockProcessingSessionId = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    FitValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnfitShredValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FitShredValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CanceledValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReissueValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockProcessingResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockProcessingResults_StockProcessingSessions_StockProcessingSessionId",
                        column: x => x.StockProcessingSessionId,
                        principalTable: "StockProcessingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockProcessingSessionStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockProcessingSessionId = table.Column<int>(type: "int", nullable: false),
                    OldStatusId = table.Column<int>(type: "int", nullable: false),
                    NewStatusId = table.Column<int>(type: "int", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Attachments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockProcessingSessionStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockProcessingSessionStatusHistory_StockProcessingSessions_StockProcessingSessionId",
                        column: x => x.StockProcessingSessionId,
                        principalTable: "StockProcessingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingBoards_BPSMachineId",
                table: "ProcessingBoards",
                column: "BPSMachineId");

            migrationBuilder.CreateIndex(
                name: "IX_StockProcessingPlans_StockMovementId",
                table: "StockProcessingPlans",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_StockProcessingPlanStatusHistory_StockProcessingPlanId",
                table: "StockProcessingPlanStatusHistory",
                column: "StockProcessingPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_StockProcessingResults_StockProcessingSessionId",
                table: "StockProcessingResults",
                column: "StockProcessingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StockProcessingSessions_DepositRequestDenominationId1",
                table: "StockProcessingSessions",
                column: "DepositRequestDenominationId1");

            migrationBuilder.CreateIndex(
                name: "IX_StockProcessingSessions_ProcessingBoardId",
                table: "StockProcessingSessions",
                column: "ProcessingBoardId");

            migrationBuilder.CreateIndex(
                name: "IX_StockProcessingSessions_StockProcessingPlanId",
                table: "StockProcessingSessions",
                column: "StockProcessingPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_StockProcessingSessionStatusHistory_StockProcessingSessionId",
                table: "StockProcessingSessionStatusHistory",
                column: "StockProcessingSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentDetailStatusHistory_AgentDetails_AgentDetailId",
                table: "AgentDetailStatusHistory",
                column: "AgentDetailId",
                principalTable: "AgentDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovedCurrencyDenominations_ApprovedCurrencyProcurements_ApprovedCurrencyId",
                table: "ApprovedCurrencyDenominations",
                column: "ApprovedCurrencyId",
                principalTable: "ApprovedCurrencyProcurements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovedCurrencyDenominations_CurrencyVendors_CurrencyVendorId",
                table: "ApprovedCurrencyDenominations",
                column: "CurrencyVendorId",
                principalTable: "CurrencyVendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovedCurrencyStatusHistory_ApprovedCurrencyProcurements_ApprovedCurrencyProcurementId",
                table: "ApprovedCurrencyStatusHistory",
                column: "ApprovedCurrencyProcurementId",
                principalTable: "ApprovedCurrencyProcurements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyVendors_VendorTypes_VendorTypeId",
                table: "CurrencyVendors",
                column: "VendorTypeId",
                principalTable: "VendorTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DepositFees_DepositRequests_DepositRequestId",
                table: "DepositFees",
                column: "DepositRequestId",
                principalTable: "DepositRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DepositFees_FeeTypes_FeeTypeId",
                table: "DepositFees",
                column: "FeeTypeId",
                principalTable: "FeeTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DepositRequestAgents_DepositRequests_DepositRequestId",
                table: "DepositRequestAgents",
                column: "DepositRequestId",
                principalTable: "DepositRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DepositRequestDenominations_DepositRequests_DepositRequestId",
                table: "DepositRequestDenominations",
                column: "DepositRequestId",
                principalTable: "DepositRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DepositRequestStatusHistory_DepositRequests_DepositRequestId",
                table: "DepositRequestStatusHistory",
                column: "DepositRequestId",
                principalTable: "DepositRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExPrintMintDetails_ExPrintMints_ExPrintMintId",
                table: "ExPrintMintDetails",
                column: "ExPrintMintId",
                principalTable: "ExPrintMints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExPrintMints_ApprovedCurrencyProcurements_ApprovedCurrencyProcurementId",
                table: "ExPrintMints",
                column: "ApprovedCurrencyProcurementId",
                principalTable: "ApprovedCurrencyProcurements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExPrintMints_CurrencyVendors_CurrencyVendorId",
                table: "ExPrintMints",
                column: "CurrencyVendorId",
                principalTable: "CurrencyVendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExPrintMintStatusHistory_ExPrintMints_ExPrintMintId",
                table: "ExPrintMintStatusHistory",
                column: "ExPrintMintId",
                principalTable: "ExPrintMints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LogisticsCategories_Logistics_LogisticsId",
                table: "LogisticsCategories",
                column: "LogisticsId",
                principalTable: "Logistics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LogisticsDetails_LogisticsCategories_LogisticsCategoryId",
                table: "LogisticsDetails",
                column: "LogisticsCategoryId",
                principalTable: "LogisticsCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LogisticsStatusHistory_Logistics_LogisticsId",
                table: "LogisticsStatusHistory",
                column: "LogisticsId",
                principalTable: "Logistics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NewStockMovementLogs_NewCaseFullStatusTypes_CaseFullStatusId",
                table: "NewStockMovementLogs",
                column: "CaseFullStatusId",
                principalTable: "NewCaseFullStatusTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NewStockMovementLogs_NewStockAccountingStatuses_AccountingStatusId",
                table: "NewStockMovementLogs",
                column: "AccountingStatusId",
                principalTable: "NewStockAccountingStatuses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NewStockMovementLogs_PackingListDetails_PackingListDetailId",
                table: "NewStockMovementLogs",
                column: "PackingListDetailId",
                principalTable: "PackingListDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NewStockMovementLogs_StockMovements_StockMovementId",
                table: "NewStockMovementLogs",
                column: "StockMovementId",
                principalTable: "StockMovements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PackingListRanges_ExPrintMintDetails_ExPrintMintDetailId",
                table: "PackingListRanges",
                column: "ExPrintMintDetailId",
                principalTable: "ExPrintMintDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PackingListRanges_ExPrintMints_ExpectedStockId",
                table: "PackingListRanges",
                column: "ExpectedStockId",
                principalTable: "ExPrintMints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedStock_ExPrintMints_ExPrintMintId",
                table: "ReceivedStock",
                column: "ExPrintMintId",
                principalTable: "ExPrintMints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedStockStatusHistory_ReceivedStock_ReceivedStockId",
                table: "ReceivedStockStatusHistory",
                column: "ReceivedStockId",
                principalTable: "ReceivedStock",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovementDenominationBoxNumberRanges_StockMovementDenominations_StockMovementDenominationId",
                table: "StockMovementDenominationBoxNumberRanges",
                column: "StockMovementDenominationId",
                principalTable: "StockMovementDenominations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovementDenominations_StockMovements_StockMovementId",
                table: "StockMovementDenominations",
                column: "StockMovementId",
                principalTable: "StockMovements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovementDiscrepancies_StockMovements_StockMovementId",
                table: "StockMovementDiscrepancies",
                column: "StockMovementId",
                principalTable: "StockMovements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovementStatusHistory_StockMovements_StockMovementId",
                table: "StockMovementStatusHistory",
                column: "StockMovementId",
                principalTable: "StockMovements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WithdrawalRequestAgents_WithdrawalRequests_WithdrawalRequestId",
                table: "WithdrawalRequestAgents",
                column: "WithdrawalRequestId",
                principalTable: "WithdrawalRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WithdrawalRequestDenominations_WithdrawalRequests_WithdrawalRequestId",
                table: "WithdrawalRequestDenominations",
                column: "WithdrawalRequestId",
                principalTable: "WithdrawalRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WithdrawalRequestStatusHistory_WithdrawalRequests_WithdrawalRequestId",
                table: "WithdrawalRequestStatusHistory",
                column: "WithdrawalRequestId",
                principalTable: "WithdrawalRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentDetailStatusHistory_AgentDetails_AgentDetailId",
                table: "AgentDetailStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovedCurrencyDenominations_ApprovedCurrencyProcurements_ApprovedCurrencyId",
                table: "ApprovedCurrencyDenominations");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovedCurrencyDenominations_CurrencyVendors_CurrencyVendorId",
                table: "ApprovedCurrencyDenominations");

            migrationBuilder.DropForeignKey(
                name: "FK_ApprovedCurrencyStatusHistory_ApprovedCurrencyProcurements_ApprovedCurrencyProcurementId",
                table: "ApprovedCurrencyStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_CurrencyVendors_VendorTypes_VendorTypeId",
                table: "CurrencyVendors");

            migrationBuilder.DropForeignKey(
                name: "FK_DepositFees_DepositRequests_DepositRequestId",
                table: "DepositFees");

            migrationBuilder.DropForeignKey(
                name: "FK_DepositFees_FeeTypes_FeeTypeId",
                table: "DepositFees");

            migrationBuilder.DropForeignKey(
                name: "FK_DepositRequestAgents_DepositRequests_DepositRequestId",
                table: "DepositRequestAgents");

            migrationBuilder.DropForeignKey(
                name: "FK_DepositRequestDenominations_DepositRequests_DepositRequestId",
                table: "DepositRequestDenominations");

            migrationBuilder.DropForeignKey(
                name: "FK_DepositRequestStatusHistory_DepositRequests_DepositRequestId",
                table: "DepositRequestStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ExPrintMintDetails_ExPrintMints_ExPrintMintId",
                table: "ExPrintMintDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_ExPrintMints_ApprovedCurrencyProcurements_ApprovedCurrencyProcurementId",
                table: "ExPrintMints");

            migrationBuilder.DropForeignKey(
                name: "FK_ExPrintMints_CurrencyVendors_CurrencyVendorId",
                table: "ExPrintMints");

            migrationBuilder.DropForeignKey(
                name: "FK_ExPrintMintStatusHistory_ExPrintMints_ExPrintMintId",
                table: "ExPrintMintStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_LogisticsCategories_Logistics_LogisticsId",
                table: "LogisticsCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_LogisticsDetails_LogisticsCategories_LogisticsCategoryId",
                table: "LogisticsDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_LogisticsStatusHistory_Logistics_LogisticsId",
                table: "LogisticsStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_NewStockMovementLogs_NewCaseFullStatusTypes_CaseFullStatusId",
                table: "NewStockMovementLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_NewStockMovementLogs_NewStockAccountingStatuses_AccountingStatusId",
                table: "NewStockMovementLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_NewStockMovementLogs_PackingListDetails_PackingListDetailId",
                table: "NewStockMovementLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_NewStockMovementLogs_StockMovements_StockMovementId",
                table: "NewStockMovementLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_PackingListRanges_ExPrintMintDetails_ExPrintMintDetailId",
                table: "PackingListRanges");

            migrationBuilder.DropForeignKey(
                name: "FK_PackingListRanges_ExPrintMints_ExpectedStockId",
                table: "PackingListRanges");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedStock_ExPrintMints_ExPrintMintId",
                table: "ReceivedStock");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedStockStatusHistory_ReceivedStock_ReceivedStockId",
                table: "ReceivedStockStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovementDenominationBoxNumberRanges_StockMovementDenominations_StockMovementDenominationId",
                table: "StockMovementDenominationBoxNumberRanges");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovementDenominations_StockMovements_StockMovementId",
                table: "StockMovementDenominations");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovementDiscrepancies_StockMovements_StockMovementId",
                table: "StockMovementDiscrepancies");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovementStatusHistory_StockMovements_StockMovementId",
                table: "StockMovementStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_WithdrawalRequestAgents_WithdrawalRequests_WithdrawalRequestId",
                table: "WithdrawalRequestAgents");

            migrationBuilder.DropForeignKey(
                name: "FK_WithdrawalRequestDenominations_WithdrawalRequests_WithdrawalRequestId",
                table: "WithdrawalRequestDenominations");

            migrationBuilder.DropForeignKey(
                name: "FK_WithdrawalRequestStatusHistory_WithdrawalRequests_WithdrawalRequestId",
                table: "WithdrawalRequestStatusHistory");

            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "StockProcessingPlanStatusHistory");

            migrationBuilder.DropTable(
                name: "StockProcessingResults");

            migrationBuilder.DropTable(
                name: "StockProcessingSessionStatusHistory");

            migrationBuilder.DropTable(
                name: "StockProcessingSessions");

            migrationBuilder.DropTable(
                name: "ProcessingBoards");

            migrationBuilder.DropTable(
                name: "StockProcessingPlans");

            migrationBuilder.DropTable(
                name: "BPSMachines");

            migrationBuilder.DropColumn(
                name: "Attachments",
                table: "DepositRequestStatusHistory");

            migrationBuilder.AlterColumn<int>(
                name: "CaseFullStatusId",
                table: "NewStockMovementLogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AccountingStatusId",
                table: "NewStockMovementLogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NewCaseFullStatus",
                table: "NewStockMovementLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NewStockAccountingStatus",
                table: "NewStockMovementLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentDetailStatusHistory_AgentDetails_AgentDetailId",
                table: "AgentDetailStatusHistory",
                column: "AgentDetailId",
                principalTable: "AgentDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovedCurrencyDenominations_ApprovedCurrencyProcurements_ApprovedCurrencyId",
                table: "ApprovedCurrencyDenominations",
                column: "ApprovedCurrencyId",
                principalTable: "ApprovedCurrencyProcurements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovedCurrencyDenominations_CurrencyVendors_CurrencyVendorId",
                table: "ApprovedCurrencyDenominations",
                column: "CurrencyVendorId",
                principalTable: "CurrencyVendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovedCurrencyStatusHistory_ApprovedCurrencyProcurements_ApprovedCurrencyProcurementId",
                table: "ApprovedCurrencyStatusHistory",
                column: "ApprovedCurrencyProcurementId",
                principalTable: "ApprovedCurrencyProcurements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyVendors_VendorTypes_VendorTypeId",
                table: "CurrencyVendors",
                column: "VendorTypeId",
                principalTable: "VendorTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DepositFees_DepositRequests_DepositRequestId",
                table: "DepositFees",
                column: "DepositRequestId",
                principalTable: "DepositRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DepositFees_FeeTypes_FeeTypeId",
                table: "DepositFees",
                column: "FeeTypeId",
                principalTable: "FeeTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DepositRequestAgents_DepositRequests_DepositRequestId",
                table: "DepositRequestAgents",
                column: "DepositRequestId",
                principalTable: "DepositRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DepositRequestDenominations_DepositRequests_DepositRequestId",
                table: "DepositRequestDenominations",
                column: "DepositRequestId",
                principalTable: "DepositRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DepositRequestStatusHistory_DepositRequests_DepositRequestId",
                table: "DepositRequestStatusHistory",
                column: "DepositRequestId",
                principalTable: "DepositRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExPrintMintDetails_ExPrintMints_ExPrintMintId",
                table: "ExPrintMintDetails",
                column: "ExPrintMintId",
                principalTable: "ExPrintMints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExPrintMints_ApprovedCurrencyProcurements_ApprovedCurrencyProcurementId",
                table: "ExPrintMints",
                column: "ApprovedCurrencyProcurementId",
                principalTable: "ApprovedCurrencyProcurements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExPrintMints_CurrencyVendors_CurrencyVendorId",
                table: "ExPrintMints",
                column: "CurrencyVendorId",
                principalTable: "CurrencyVendors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExPrintMintStatusHistory_ExPrintMints_ExPrintMintId",
                table: "ExPrintMintStatusHistory",
                column: "ExPrintMintId",
                principalTable: "ExPrintMints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LogisticsCategories_Logistics_LogisticsId",
                table: "LogisticsCategories",
                column: "LogisticsId",
                principalTable: "Logistics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LogisticsDetails_LogisticsCategories_LogisticsCategoryId",
                table: "LogisticsDetails",
                column: "LogisticsCategoryId",
                principalTable: "LogisticsCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LogisticsStatusHistory_Logistics_LogisticsId",
                table: "LogisticsStatusHistory",
                column: "LogisticsId",
                principalTable: "Logistics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NewStockMovementLogs_NewCaseFullStatusTypes_CaseFullStatusId",
                table: "NewStockMovementLogs",
                column: "CaseFullStatusId",
                principalTable: "NewCaseFullStatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NewStockMovementLogs_NewStockAccountingStatuses_AccountingStatusId",
                table: "NewStockMovementLogs",
                column: "AccountingStatusId",
                principalTable: "NewStockAccountingStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NewStockMovementLogs_PackingListDetails_PackingListDetailId",
                table: "NewStockMovementLogs",
                column: "PackingListDetailId",
                principalTable: "PackingListDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NewStockMovementLogs_StockMovements_StockMovementId",
                table: "NewStockMovementLogs",
                column: "StockMovementId",
                principalTable: "StockMovements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PackingListRanges_ExPrintMintDetails_ExPrintMintDetailId",
                table: "PackingListRanges",
                column: "ExPrintMintDetailId",
                principalTable: "ExPrintMintDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PackingListRanges_ExPrintMints_ExpectedStockId",
                table: "PackingListRanges",
                column: "ExpectedStockId",
                principalTable: "ExPrintMints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedStock_ExPrintMints_ExPrintMintId",
                table: "ReceivedStock",
                column: "ExPrintMintId",
                principalTable: "ExPrintMints",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedStockStatusHistory_ReceivedStock_ReceivedStockId",
                table: "ReceivedStockStatusHistory",
                column: "ReceivedStockId",
                principalTable: "ReceivedStock",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovementDenominationBoxNumberRanges_StockMovementDenominations_StockMovementDenominationId",
                table: "StockMovementDenominationBoxNumberRanges",
                column: "StockMovementDenominationId",
                principalTable: "StockMovementDenominations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovementDenominations_StockMovements_StockMovementId",
                table: "StockMovementDenominations",
                column: "StockMovementId",
                principalTable: "StockMovements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovementDiscrepancies_StockMovements_StockMovementId",
                table: "StockMovementDiscrepancies",
                column: "StockMovementId",
                principalTable: "StockMovements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovementStatusHistory_StockMovements_StockMovementId",
                table: "StockMovementStatusHistory",
                column: "StockMovementId",
                principalTable: "StockMovements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WithdrawalRequestAgents_WithdrawalRequests_WithdrawalRequestId",
                table: "WithdrawalRequestAgents",
                column: "WithdrawalRequestId",
                principalTable: "WithdrawalRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WithdrawalRequestDenominations_WithdrawalRequests_WithdrawalRequestId",
                table: "WithdrawalRequestDenominations",
                column: "WithdrawalRequestId",
                principalTable: "WithdrawalRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WithdrawalRequestStatusHistory_WithdrawalRequests_WithdrawalRequestId",
                table: "WithdrawalRequestStatusHistory",
                column: "WithdrawalRequestId",
                principalTable: "WithdrawalRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
