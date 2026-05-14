using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommercialBankId = table.Column<int>(type: "int", nullable: false),
                    CommercialBankBranchId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    AgentNames = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NIN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PassportPhoto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkIdScan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SignatureScan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovalAttachment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentDetails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovedCurrencyProcurements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BoardApprovalAttachment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovedCurrencyProcurements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DepositRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionRefNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CbId = table.Column<int>(type: "int", nullable: false),
                    cbUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CbCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InitiatorUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupervisorUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CbBranchId = table.Column<int>(type: "int", nullable: false),
                    BouBranchId = table.Column<int>(type: "int", nullable: false),
                    DepositDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RtgsDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RtgsCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresSpecialApproval = table.Column<bool>(type: "bit", nullable: false),
                    SpecialApprovalReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusTypeId = table.Column<int>(type: "int", nullable: false),
                    TotalNotesAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCoinsAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Logistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeamLeader = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssistantTeamLeader = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewCaseFullStatusTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewCaseFullStatusTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewStockAccountingStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountingStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountingStatusDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewStockAccountingStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReceivedStatusTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivedStatusTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VendorTypes",
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
                    table.PrimaryKey("PK_VendorTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WithdrawalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionRefNumber = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CbId = table.Column<int>(type: "int", nullable: false),
                    CbCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BouBranchId = table.Column<int>(type: "int", nullable: false),
                    BouBranchCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CollectionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RtgsDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RtgsCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresSpecialApproval = table.Column<bool>(type: "bit", nullable: false),
                    SpecialApprovalReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusTypeId = table.Column<int>(type: "int", nullable: false),
                    StatusTypeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalNotesAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCoinsAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsLumpSum = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithdrawalRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentDetailStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentDetailId = table.Column<int>(type: "int", nullable: false),
                    OldStatusId = table.Column<int>(type: "int", nullable: false),
                    NewStatusId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AgentDetailStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentDetailStatusHistory_AgentDetails_AgentDetailId",
                        column: x => x.AgentDetailId,
                        principalTable: "AgentDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovedCurrencyStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovedCurrencyProcurementId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ApprovedCurrencyStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovedCurrencyStatusHistory_ApprovedCurrencyProcurements_ApprovedCurrencyProcurementId",
                        column: x => x.ApprovedCurrencyProcurementId,
                        principalTable: "ApprovedCurrencyProcurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DepositRequestAgents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepositRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentId = table.Column<int>(type: "int", nullable: false),
                    EnteredBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttachmentData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    AttachmentFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentRemarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositRequestAgents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepositRequestAgents_DepositRequests_DepositRequestId",
                        column: x => x.DepositRequestId,
                        principalTable: "DepositRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DepositRequestDenominations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepositRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    DenominationValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DenominationTypeId = table.Column<int>(type: "int", nullable: false),
                    Bundles = table.Column<int>(type: "int", nullable: true),
                    Boxes = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubCategoryId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositRequestDenominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepositRequestDenominations_DepositRequests_DepositRequestId",
                        column: x => x.DepositRequestId,
                        principalTable: "DepositRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DepositRequestStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepositRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldStatusId = table.Column<int>(type: "int", nullable: false),
                    NewStatusId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_DepositRequestStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepositRequestStatusHistory_DepositRequests_DepositRequestId",
                        column: x => x.DepositRequestId,
                        principalTable: "DepositRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LogisticsCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogisticsId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogisticsCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogisticsCategories_Logistics_LogisticsId",
                        column: x => x.LogisticsId,
                        principalTable: "Logistics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LogisticsStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogisticsId = table.Column<int>(type: "int", nullable: false),
                    OldStatusId = table.Column<int>(type: "int", nullable: false),
                    NewStatusId = table.Column<int>(type: "int", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogisticsStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogisticsStatusHistory_Logistics_LogisticsId",
                        column: x => x.LogisticsId,
                        principalTable: "Logistics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MovementType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: true),
                    CurrentStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DispatchingBranchId = table.Column<int>(type: "int", nullable: true),
                    ReceivingBranchId = table.Column<int>(type: "int", nullable: true),
                    CommercialBankBranchId = table.Column<int>(type: "int", nullable: true),
                    LogisticsId = table.Column<int>(type: "int", nullable: true),
                    TotalBagsBoxes = table.Column<int>(type: "int", nullable: true),
                    GrandTotalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_Logistics_LogisticsId",
                        column: x => x.LogisticsId,
                        principalTable: "Logistics",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CurrencyVendors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VendorTypeId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyVendors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrencyVendors_VendorTypes_VendorTypeId",
                        column: x => x.VendorTypeId,
                        principalTable: "VendorTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WithdrawalRequestAgents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WithdrawalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentId = table.Column<int>(type: "int", nullable: false),
                    EnteredBy = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AttachmentData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    AttachmentFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttachmentRemarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithdrawalRequestAgents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WithdrawalRequestAgents_WithdrawalRequests_WithdrawalRequestId",
                        column: x => x.WithdrawalRequestId,
                        principalTable: "WithdrawalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WithdrawalRequestDenominations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WithdrawalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    DenominationValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DenominationTypeId = table.Column<int>(type: "int", nullable: false),
                    Bundles = table.Column<int>(type: "int", nullable: true),
                    Boxes = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithdrawalRequestDenominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WithdrawalRequestDenominations_WithdrawalRequests_WithdrawalRequestId",
                        column: x => x.WithdrawalRequestId,
                        principalTable: "WithdrawalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WithdrawalRequestStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WithdrawalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_WithdrawalRequestStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WithdrawalRequestStatusHistory_WithdrawalRequests_WithdrawalRequestId",
                        column: x => x.WithdrawalRequestId,
                        principalTable: "WithdrawalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LogisticsDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogisticsCategoryId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Frequency = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogisticsDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogisticsDetails_LogisticsCategories_LogisticsCategoryId",
                        column: x => x.LogisticsCategoryId,
                        principalTable: "LogisticsCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovementDenominations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    NumBagsBoxes = table.Column<int>(type: "int", nullable: true),
                    NumBundles = table.Column<int>(type: "int", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    Categorization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContainerType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubTotalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DenominationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StockMovementId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovementDenominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovementDenominations_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovementDiscrepancies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockMovementId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovementDiscrepancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovementDiscrepancies_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovementStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockMovementId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_StockMovementStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovementStatusHistory_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovedCurrencyDenominations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    ApprovedCurrencyId = table.Column<int>(type: "int", nullable: false),
                    NumberOfPieces = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CurrencyVendorId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovedCurrencyDenominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovedCurrencyDenominations_ApprovedCurrencyProcurements_ApprovedCurrencyId",
                        column: x => x.ApprovedCurrencyId,
                        principalTable: "ApprovedCurrencyProcurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApprovedCurrencyDenominations_CurrencyVendors_CurrencyVendorId",
                        column: x => x.CurrencyVendorId,
                        principalTable: "CurrencyVendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExPrintMints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExPrintMintReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrencyVendorId = table.Column<int>(type: "int", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    DeliveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Attachment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedCurrencyProcurementId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExPrintMints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExPrintMints_ApprovedCurrencyProcurements_ApprovedCurrencyProcurementId",
                        column: x => x.ApprovedCurrencyProcurementId,
                        principalTable: "ApprovedCurrencyProcurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExPrintMints_CurrencyVendors_CurrencyVendorId",
                        column: x => x.CurrencyVendorId,
                        principalTable: "CurrencyVendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovementDenominationBoxNumberRanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Prefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Start = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    End = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StockMovementDenominationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovementDenominationBoxNumberRanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovementDenominationBoxNumberRanges_StockMovementDenominations_StockMovementDenominationId",
                        column: x => x.StockMovementDenominationId,
                        principalTable: "StockMovementDenominations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExPrintMintDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExPrintMintId = table.Column<int>(type: "int", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    NumberOfPieces = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NumberOfBoxes = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExPrintMintDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExPrintMintDetails_ExPrintMints_ExPrintMintId",
                        column: x => x.ExPrintMintId,
                        principalTable: "ExPrintMints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExPrintMintStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExPrintMintId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ExPrintMintStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExPrintMintStatusHistory_ExPrintMints_ExPrintMintId",
                        column: x => x.ExPrintMintId,
                        principalTable: "ExPrintMints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReceivedStock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExPrintMintId = table.Column<int>(type: "int", nullable: false),
                    DateDelivered = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveryAirline = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TestingFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TestingFeesCurrency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShippingFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ShippingFeesCurrency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InsuranceFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InsuranceFeesCurrency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HandlingCharges = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HandlingChargesCurrency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClearingFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClearingFeesCurrency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsPartial = table.Column<bool>(type: "bit", nullable: true),
                    CurrentStatusName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumberOfBoxes = table.Column<int>(type: "int", nullable: false),
                    ReceivedStatusTypesId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivedStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceivedStock_ExPrintMints_ExPrintMintId",
                        column: x => x.ExPrintMintId,
                        principalTable: "ExPrintMints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceivedStock_ReceivedStatusTypes_ReceivedStatusTypesId",
                        column: x => x.ReceivedStatusTypesId,
                        principalTable: "ReceivedStatusTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PackingListRanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExPrintMintDetailId = table.Column<int>(type: "int", nullable: false),
                    CaseNumberPrefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartCaseNumber = table.Column<int>(type: "int", nullable: false),
                    EndCaseNumber = table.Column<int>(type: "int", nullable: false),
                    SNPrefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SNRangeStart = table.Column<int>(type: "int", nullable: false),
                    SNRangeEnd = table.Column<int>(type: "int", nullable: false),
                    ExpectedStockId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackingListRanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackingListRanges_ExPrintMintDetails_ExPrintMintDetailId",
                        column: x => x.ExPrintMintDetailId,
                        principalTable: "ExPrintMintDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackingListRanges_ExPrintMints_ExpectedStockId",
                        column: x => x.ExpectedStockId,
                        principalTable: "ExPrintMints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReceivedStockStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceivedStockId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ReceivedStockStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceivedStockStatusHistory_ReceivedStock_ReceivedStockId",
                        column: x => x.ReceivedStockId,
                        principalTable: "ReceivedStock",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PackingListDetails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackingListRangeId = table.Column<int>(type: "int", nullable: false),
                    CaseNumber = table.Column<int>(type: "int", nullable: false),
                    SerialNoteStartNumber = table.Column<int>(type: "int", nullable: false),
                    SerialNoteEndNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackingListDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackingListDetails_PackingListRanges_PackingListRangeId",
                        column: x => x.PackingListRangeId,
                        principalTable: "PackingListRanges",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReceivedStockDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceivedStockId = table.Column<int>(type: "int", nullable: false),
                    PackingListRangeId = table.Column<int>(type: "int", nullable: false),
                    ReceivedStartCaseNumber = table.Column<int>(type: "int", nullable: false),
                    ReceivedEndCaseNumber = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivedStockDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceivedStockDetails_PackingListRanges_PackingListRangeId",
                        column: x => x.PackingListRangeId,
                        principalTable: "PackingListRanges",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReceivedStockDetails_ReceivedStock_ReceivedStockId",
                        column: x => x.ReceivedStockId,
                        principalTable: "ReceivedStock",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NewStockMovementLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockMovementId = table.Column<int>(type: "int", nullable: false),
                    PackingListDetailId = table.Column<long>(type: "bigint", nullable: false),
                    SerialNoteStartNumber = table.Column<int>(type: "int", nullable: false),
                    SerialNoteEndNumber = table.Column<int>(type: "int", nullable: false),
                    AccountingStatusId = table.Column<int>(type: "int", nullable: true),
                    CaseFullStatusId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewStockMovementLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewStockMovementLogs_NewCaseFullStatusTypes_CaseFullStatusId",
                        column: x => x.CaseFullStatusId,
                        principalTable: "NewCaseFullStatusTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NewStockMovementLogs_NewStockAccountingStatuses_AccountingStatusId",
                        column: x => x.AccountingStatusId,
                        principalTable: "NewStockAccountingStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NewStockMovementLogs_PackingListDetails_PackingListDetailId",
                        column: x => x.PackingListDetailId,
                        principalTable: "PackingListDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NewStockMovementLogs_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentDetailStatusHistory_AgentDetailId",
                table: "AgentDetailStatusHistory",
                column: "AgentDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedCurrencyDenominations_ApprovedCurrencyId",
                table: "ApprovedCurrencyDenominations",
                column: "ApprovedCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedCurrencyDenominations_CurrencyVendorId",
                table: "ApprovedCurrencyDenominations",
                column: "CurrencyVendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedCurrencyStatusHistory_ApprovedCurrencyProcurementId",
                table: "ApprovedCurrencyStatusHistory",
                column: "ApprovedCurrencyProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyVendors_VendorTypeId",
                table: "CurrencyVendors",
                column: "VendorTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DepositRequestAgents_DepositRequestId",
                table: "DepositRequestAgents",
                column: "DepositRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DepositRequestDenominations_DepositRequestId",
                table: "DepositRequestDenominations",
                column: "DepositRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DepositRequestStatusHistory_DepositRequestId",
                table: "DepositRequestStatusHistory",
                column: "DepositRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMintDetails_ExPrintMintId",
                table: "ExPrintMintDetails",
                column: "ExPrintMintId");

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMints_ApprovedCurrencyProcurementId",
                table: "ExPrintMints",
                column: "ApprovedCurrencyProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMints_CurrencyVendorId",
                table: "ExPrintMints",
                column: "CurrencyVendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMintStatusHistory_ExPrintMintId",
                table: "ExPrintMintStatusHistory",
                column: "ExPrintMintId");

            migrationBuilder.CreateIndex(
                name: "IX_LogisticsCategories_LogisticsId",
                table: "LogisticsCategories",
                column: "LogisticsId");

            migrationBuilder.CreateIndex(
                name: "IX_LogisticsDetails_LogisticsCategoryId",
                table: "LogisticsDetails",
                column: "LogisticsCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_LogisticsStatusHistory_LogisticsId",
                table: "LogisticsStatusHistory",
                column: "LogisticsId");

            migrationBuilder.CreateIndex(
                name: "IX_NewStockMovementLogs_AccountingStatusId",
                table: "NewStockMovementLogs",
                column: "AccountingStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_NewStockMovementLogs_CaseFullStatusId",
                table: "NewStockMovementLogs",
                column: "CaseFullStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_NewStockMovementLogs_PackingListDetailId",
                table: "NewStockMovementLogs",
                column: "PackingListDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_NewStockMovementLogs_StockMovementId",
                table: "NewStockMovementLogs",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_PackingListDetails_PackingListRangeId",
                table: "PackingListDetails",
                column: "PackingListRangeId");

            migrationBuilder.CreateIndex(
                name: "IX_PackingListRanges_ExpectedStockId",
                table: "PackingListRanges",
                column: "ExpectedStockId");

            migrationBuilder.CreateIndex(
                name: "IX_PackingListRanges_ExPrintMintDetailId",
                table: "PackingListRanges",
                column: "ExPrintMintDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStock_ExPrintMintId",
                table: "ReceivedStock",
                column: "ExPrintMintId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStock_ReceivedStatusTypesId",
                table: "ReceivedStock",
                column: "ReceivedStatusTypesId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockDetails_PackingListRangeId",
                table: "ReceivedStockDetails",
                column: "PackingListRangeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockDetails_ReceivedStockId",
                table: "ReceivedStockDetails",
                column: "ReceivedStockId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockStatusHistory_ReceivedStockId",
                table: "ReceivedStockStatusHistory",
                column: "ReceivedStockId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementDenominationBoxNumberRanges_StockMovementDenominationId",
                table: "StockMovementDenominationBoxNumberRanges",
                column: "StockMovementDenominationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementDenominations_StockMovementId",
                table: "StockMovementDenominations",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementDiscrepancies_StockMovementId",
                table: "StockMovementDiscrepancies",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_LogisticsId",
                table: "StockMovements",
                column: "LogisticsId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementStatusHistory_StockMovementId",
                table: "StockMovementStatusHistory",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequestAgents_WithdrawalRequestId_EnteredBy",
                table: "WithdrawalRequestAgents",
                columns: new[] { "WithdrawalRequestId", "EnteredBy" });

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequestDenominations_WithdrawalRequestId_Origin",
                table: "WithdrawalRequestDenominations",
                columns: new[] { "WithdrawalRequestId", "Origin" });

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_CbId",
                table: "WithdrawalRequests",
                column: "CbId");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_CbId_BouBranchId_CollectionDate",
                table: "WithdrawalRequests",
                columns: new[] { "CbId", "BouBranchId", "CollectionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_CollectionDate",
                table: "WithdrawalRequests",
                column: "CollectionDate");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_StatusTypeId",
                table: "WithdrawalRequests",
                column: "StatusTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequests_TransactionRefNumber",
                table: "WithdrawalRequests",
                column: "TransactionRefNumber",
                unique: true,
                filter: "[TransactionRefNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequestStatusHistory_WithdrawalRequestId",
                table: "WithdrawalRequestStatusHistory",
                column: "WithdrawalRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentDetailStatusHistory");

            migrationBuilder.DropTable(
                name: "ApprovedCurrencyDenominations");

            migrationBuilder.DropTable(
                name: "ApprovedCurrencyStatusHistory");

            migrationBuilder.DropTable(
                name: "DepositRequestAgents");

            migrationBuilder.DropTable(
                name: "DepositRequestDenominations");

            migrationBuilder.DropTable(
                name: "DepositRequestStatusHistory");

            migrationBuilder.DropTable(
                name: "ExPrintMintStatusHistory");

            migrationBuilder.DropTable(
                name: "LogisticsDetails");

            migrationBuilder.DropTable(
                name: "LogisticsStatusHistory");

            migrationBuilder.DropTable(
                name: "NewStockMovementLogs");

            migrationBuilder.DropTable(
                name: "ReceivedStockDetails");

            migrationBuilder.DropTable(
                name: "ReceivedStockStatusHistory");

            migrationBuilder.DropTable(
                name: "StockMovementDenominationBoxNumberRanges");

            migrationBuilder.DropTable(
                name: "StockMovementDiscrepancies");

            migrationBuilder.DropTable(
                name: "StockMovementStatusHistory");

            migrationBuilder.DropTable(
                name: "WithdrawalRequestAgents");

            migrationBuilder.DropTable(
                name: "WithdrawalRequestDenominations");

            migrationBuilder.DropTable(
                name: "WithdrawalRequestStatusHistory");

            migrationBuilder.DropTable(
                name: "AgentDetails");

            migrationBuilder.DropTable(
                name: "DepositRequests");

            migrationBuilder.DropTable(
                name: "LogisticsCategories");

            migrationBuilder.DropTable(
                name: "NewCaseFullStatusTypes");

            migrationBuilder.DropTable(
                name: "NewStockAccountingStatuses");

            migrationBuilder.DropTable(
                name: "PackingListDetails");

            migrationBuilder.DropTable(
                name: "ReceivedStock");

            migrationBuilder.DropTable(
                name: "StockMovementDenominations");

            migrationBuilder.DropTable(
                name: "WithdrawalRequests");

            migrationBuilder.DropTable(
                name: "PackingListRanges");

            migrationBuilder.DropTable(
                name: "ReceivedStatusTypes");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "ExPrintMintDetails");

            migrationBuilder.DropTable(
                name: "Logistics");

            migrationBuilder.DropTable(
                name: "ExPrintMints");

            migrationBuilder.DropTable(
                name: "ApprovedCurrencyProcurements");

            migrationBuilder.DropTable(
                name: "CurrencyVendors");

            migrationBuilder.DropTable(
                name: "VendorTypes");
        }
    }
}
