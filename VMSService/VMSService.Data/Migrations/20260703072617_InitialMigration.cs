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
                    CommercialBankBranchId = table.Column<int>(type: "int", nullable: true),
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
                name: "Attachments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    WorkflowTransitionId = table.Column<int>(type: "int", nullable: false),
                    TransitionAttachmentRequirementId = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                name: "BankingShortages",
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
                    table.PrimaryKey("PK_BankingShortages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankingSurpluses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DepositDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_BankingSurpluses", x => x.Id);
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
                name: "CountingRooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountingRooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DepositProcessingFees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EndValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MedianValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Charge = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DenominationTypeId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositProcessingFees", x => x.Id);
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
                name: "FeeTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeTypes", x => x.Id);
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
                name: "MinDepositStandards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    MinimumBundles = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinDepositStandards", x => x.Id);
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
                    EftDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                name: "Vaults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_Vaults", x => x.Id);
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
                    FundSettlementValueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RtgsCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresSpecialApproval = table.Column<bool>(type: "bit", nullable: false),
                    SpecialApprovalReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusTypeId = table.Column<int>(type: "int", nullable: false),
                    StatusTypeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalNotesAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCoinsAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsLumpSum = table.Column<bool>(type: "bit", nullable: false),
                    RtgsVerificationStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RtgsVerifiedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RecallRtgsPending = table.Column<bool>(type: "bit", nullable: false),
                    RecallRtgsCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecallRtgsDate = table.Column<DateOnly>(type: "date", nullable: true),
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
                    AttachmentIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    AttachmentIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                name: "BankingShortageStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankingShortageId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_BankingShortageStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankingShortageStatusHistories_BankingShortages_BankingShortageId",
                        column: x => x.BankingShortageId,
                        principalTable: "BankingShortages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankingSurplusStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankingSurplusId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_BankingSurplusStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankingSurplusStatusHistories_BankingSurpluses_BankingSurplusId",
                        column: x => x.BankingSurplusId,
                        principalTable: "BankingSurpluses",
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
                    CountingRoomId = table.Column<int>(type: "int", nullable: false),
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
                    table.ForeignKey(
                        name: "FK_ProcessingBoards_CountingRooms_CountingRoomId",
                        column: x => x.CountingRoomId,
                        principalTable: "CountingRooms",
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
                    AttachmentIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                name: "DepositFees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FeeTypeId = table.Column<int>(type: "int", nullable: false),
                    DepositRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsWaived = table.Column<bool>(type: "bit", nullable: false),
                    WaiverReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WaivedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WaivedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepositFees_DepositRequests_DepositRequestId",
                        column: x => x.DepositRequestId,
                        principalTable: "DepositRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DepositFees_FeeTypes_FeeTypeId",
                        column: x => x.FeeTypeId,
                        principalTable: "FeeTypes",
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
                name: "VaultSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VaultId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaultSections_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    BundleType = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    AttachmentIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                name: "StockProcessingPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProcessingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    StockMovementId = table.Column<int>(type: "int", nullable: true),
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
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Bags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CbCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VaultSectionId = table.Column<int>(type: "int", nullable: true),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    SubCategoryId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bags_VaultSections_VaultSectionId",
                        column: x => x.VaultSectionId,
                        principalTable: "VaultSections",
                        principalColumn: "Id");
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
                name: "CountingRoomSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountingRoomId = table.Column<int>(type: "int", nullable: false),
                    StockProcessingPlanId = table.Column<int>(type: "int", nullable: false),
                    BoardSupervisorUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountingRoomSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CountingRoomSessions_CountingRooms_CountingRoomId",
                        column: x => x.CountingRoomId,
                        principalTable: "CountingRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CountingRoomSessions_StockProcessingPlans_StockProcessingPlanId",
                        column: x => x.StockProcessingPlanId,
                        principalTable: "StockProcessingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    IsManual = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockProcessingSessions", x => x.Id);
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
                name: "BagDenominations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BagId = table.Column<int>(type: "int", nullable: false),
                    DepositRequestDenominationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    NumberOfBundlesOrBoxes = table.Column<int>(type: "int", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "BaggedBags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentBagId = table.Column<int>(type: "int", nullable: false),
                    ChildBagId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaggedBags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaggedBags_Bags_ChildBagId",
                        column: x => x.ChildBagId,
                        principalTable: "Bags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaggedBags_Bags_ParentBagId",
                        column: x => x.ParentBagId,
                        principalTable: "Bags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BagMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BagId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: true),
                    OldVaultSectionId = table.Column<int>(type: "int", nullable: false),
                    NewVaultSectionId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BagMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BagMovements_Bags_BagId",
                        column: x => x.BagId,
                        principalTable: "Bags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BagMovements_VaultSections_NewVaultSectionId",
                        column: x => x.NewVaultSectionId,
                        principalTable: "VaultSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BagMovements_VaultSections_OldVaultSectionId",
                        column: x => x.OldVaultSectionId,
                        principalTable: "VaultSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionBags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BagId = table.Column<int>(type: "int", nullable: false),
                    StockProcessingPlanId = table.Column<int>(type: "int", nullable: false),
                    ProcessingBoardId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionBags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionBags_Bags_BagId",
                        column: x => x.BagId,
                        principalTable: "Bags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionBags_ProcessingBoards_ProcessingBoardId",
                        column: x => x.ProcessingBoardId,
                        principalTable: "ProcessingBoards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionBags_StockProcessingPlans_StockProcessingPlanId",
                        column: x => x.StockProcessingPlanId,
                        principalTable: "StockProcessingPlans",
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
                    AttachmentIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                name: "BoardSessionSurplusShortages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommercialBankId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    SessionSourceBranch = table.Column<int>(type: "int", nullable: true),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsSurplus = table.Column<bool>(type: "bit", nullable: false),
                    IsDifferentDenomination = table.Column<bool>(type: "bit", nullable: false),
                    IsProcessedOnRtgs = table.Column<bool>(type: "bit", nullable: false),
                    RtgsCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepositRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                name: "ProcessingSessionBags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BagId = table.Column<int>(type: "int", nullable: false),
                    StockProcessingSessionId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessingSessionBags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessingSessionBags_Bags_BagId",
                        column: x => x.BagId,
                        principalTable: "Bags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessingSessionBags_StockProcessingSessions_StockProcessingSessionId",
                        column: x => x.StockProcessingSessionId,
                        principalTable: "StockProcessingSessions",
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

            migrationBuilder.CreateTable(
                name: "BagMovementStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BagMovementId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_BagMovementStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BagMovementStatusHistory_BagMovements_BagMovementId",
                        column: x => x.BagMovementId,
                        principalTable: "BagMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    AttachmentIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                name: "BoardSessionDifferentDenoms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BoardSessionSurplusShortageId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardSessionDifferentDenoms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoardSessionDifferentDenoms_BoardSessionSurplusShortages_BoardSessionSurplusShortageId",
                        column: x => x.BoardSessionSurplusShortageId,
                        principalTable: "BoardSessionSurplusShortages",
                        principalColumn: "Id");
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
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateTable(
                name: "MonthlyShortageReimbursements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MonthlyShortageId = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_MonthlyShortageReimbursements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyShortageReimbursements_MonthlyShortages_MonthlyShortageId",
                        column: x => x.MonthlyShortageId,
                        principalTable: "MonthlyShortages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyShortageReimbursementDenominations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthlyShortageReimbursementId = table.Column<int>(type: "int", nullable: false),
                    DenominationId = table.Column<int>(type: "int", nullable: false),
                    DenominationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DenominationValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NumberOfPieces = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyShortageReimbursementDenominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyShortageReimbursementDenominations_MonthlyShortageReimbursements_MonthlyShortageReimbursementId",
                        column: x => x.MonthlyShortageReimbursementId,
                        principalTable: "MonthlyShortageReimbursements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyShortageReimbursementStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthlyShortageReimbursementId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_MonthlyShortageReimbursementStatusHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockMovementDenominationBags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockMovementDenominationId = table.Column<int>(type: "int", nullable: false),
                    BagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovementDenominationBags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovementDenominationBags_Bags_BagId",
                        column: x => x.BagId,
                        principalTable: "Bags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovementDenominationBags_StockMovementDenominations_StockMovementDenominationId",
                        column: x => x.StockMovementDenominationId,
                        principalTable: "StockMovementDenominations",
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
                name: "IX_BagDenominations_BagId",
                table: "BagDenominations",
                column: "BagId");

            migrationBuilder.CreateIndex(
                name: "IX_BagDenominations_DepositRequestDenominationId",
                table: "BagDenominations",
                column: "DepositRequestDenominationId");

            migrationBuilder.CreateIndex(
                name: "IX_BaggedBags_ChildBagId",
                table: "BaggedBags",
                column: "ChildBagId");

            migrationBuilder.CreateIndex(
                name: "IX_BaggedBags_ParentBagId",
                table: "BaggedBags",
                column: "ParentBagId");

            migrationBuilder.CreateIndex(
                name: "IX_BagMovements_BagId",
                table: "BagMovements",
                column: "BagId");

            migrationBuilder.CreateIndex(
                name: "IX_BagMovements_NewVaultSectionId",
                table: "BagMovements",
                column: "NewVaultSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BagMovements_OldVaultSectionId",
                table: "BagMovements",
                column: "OldVaultSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BagMovementStatusHistory_BagMovementId",
                table: "BagMovementStatusHistory",
                column: "BagMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_Bags_VaultSectionId",
                table: "Bags",
                column: "VaultSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BankingShortageStatusHistories_BankingShortageId",
                table: "BankingShortageStatusHistories",
                column: "BankingShortageId");

            migrationBuilder.CreateIndex(
                name: "IX_BankingSurplusStatusHistories_BankingSurplusId",
                table: "BankingSurplusStatusHistories",
                column: "BankingSurplusId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSessionDifferentDenoms_BoardSessionSurplusShortageId",
                table: "BoardSessionDifferentDenoms",
                column: "BoardSessionSurplusShortageId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSessionSurplusShortages_DepositRequestId",
                table: "BoardSessionSurplusShortages",
                column: "DepositRequestId");

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
                name: "IX_CountingRoomSessions_CountingRoomId",
                table: "CountingRoomSessions",
                column: "CountingRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_CountingRoomSessions_StockProcessingPlanId",
                table: "CountingRoomSessions",
                column: "StockProcessingPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyVendors_VendorTypeId",
                table: "CurrencyVendors",
                column: "VendorTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DepositFees_DepositRequestId",
                table: "DepositFees",
                column: "DepositRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DepositFees_FeeTypeId",
                table: "DepositFees",
                column: "FeeTypeId");

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
                name: "IX_ProcessingBoards_BPSMachineId",
                table: "ProcessingBoards",
                column: "BPSMachineId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingBoards_CountingRoomId",
                table: "ProcessingBoards",
                column: "CountingRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingSessionBags_BagId",
                table: "ProcessingSessionBags",
                column: "BagId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingSessionBags_StockProcessingSessionId",
                table: "ProcessingSessionBags",
                column: "StockProcessingSessionId");

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
                name: "IX_SessionBags_BagId",
                table: "SessionBags",
                column: "BagId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionBags_ProcessingBoardId",
                table: "SessionBags",
                column: "ProcessingBoardId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionBags_StockProcessingPlanId",
                table: "SessionBags",
                column: "StockProcessingPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementDenominationBoxNumberRanges_StockMovementDenominationId",
                table: "StockMovementDenominationBoxNumberRanges",
                column: "StockMovementDenominationId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyShortageReimbursementDenominations_MonthlyShortageReimbursementId",
                table: "MonthlyShortageReimbursementDenominations",
                column: "MonthlyShortageReimbursementId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyShortageReimbursements_MonthlyShortageId",
                table: "MonthlyShortageReimbursements",
                column: "MonthlyShortageId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementDenominationBags_BagId",
                table: "StockMovementDenominationBags",
                column: "BagId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementDenominationBags_StockMovementDenominationId",
                table: "StockMovementDenominationBags",
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

            migrationBuilder.CreateIndex(
                name: "IX_VaultSections_VaultId",
                table: "VaultSections",
                column: "VaultId");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequestAgents_WithdrawalRequestId_EnteredBy",
                table: "WithdrawalRequestAgents",
                columns: new[] { "WithdrawalRequestId", "EnteredBy" });

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequestDenominationBoxes_CaseNumber",
                table: "WithdrawalRequestDenominationBoxes",
                column: "CaseNumber");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequestDenominationBoxes_WithdrawalRequestDenominationId",
                table: "WithdrawalRequestDenominationBoxes",
                column: "WithdrawalRequestDenominationId");

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
                name: "MonthlyShortageReimbursementDenominations");

            migrationBuilder.DropTable(
                name: "MonthlyShortageReimbursementStatusHistories");

            migrationBuilder.DropTable(
                name: "StockMovementDenominationBags");

            migrationBuilder.DropTable(
                name: "MonthlyShortageReimbursements");

            migrationBuilder.DropTable(
                name: "AgentDetailStatusHistory");

            migrationBuilder.DropTable(
                name: "ApprovedCurrencyDenominations");

            migrationBuilder.DropTable(
                name: "ApprovedCurrencyStatusHistory");

            migrationBuilder.DropTable(
                name: "Attachments");

            migrationBuilder.DropTable(
                name: "BagDenominations");

            migrationBuilder.DropTable(
                name: "BaggedBags");

            migrationBuilder.DropTable(
                name: "BagMovementStatusHistory");

            migrationBuilder.DropTable(
                name: "BankingShortageStatusHistories");

            migrationBuilder.DropTable(
                name: "BankingSurplusStatusHistories");

            migrationBuilder.DropTable(
                name: "BoardSessionDifferentDenoms");

            migrationBuilder.DropTable(
                name: "BoxStates");

            migrationBuilder.DropTable(
                name: "CountingRoomSessions");

            migrationBuilder.DropTable(
                name: "DepositFees");

            migrationBuilder.DropTable(
                name: "DepositProcessingFees");

            migrationBuilder.DropTable(
                name: "DepositRequestAgents");

            migrationBuilder.DropTable(
                name: "DepositRequestStatusHistory");

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
                name: "ExPrintMintStatusHistory");

            migrationBuilder.DropTable(
                name: "LogisticsDetails");

            migrationBuilder.DropTable(
                name: "LogisticsStatusHistory");

            migrationBuilder.DropTable(
                name: "MinDepositStandards");

            migrationBuilder.DropTable(
                name: "MonthlyCommercialBankSettlementStatusHistories");

            migrationBuilder.DropTable(
                name: "MonthlyShortageDetails");

            migrationBuilder.DropTable(
                name: "MonthlyShortageStatusHistories");

            migrationBuilder.DropTable(
                name: "MonthlySurplusStatusHistories");

            migrationBuilder.DropTable(
                name: "NewStockMovementLogs");

            migrationBuilder.DropTable(
                name: "ProcessingSessionBags");

            migrationBuilder.DropTable(
                name: "ReceivedStockDetails");

            migrationBuilder.DropTable(
                name: "ReceivedStockStatusHistory");

            migrationBuilder.DropTable(
                name: "SessionBags");

            migrationBuilder.DropTable(
                name: "StockMovementDenominationBoxNumberRanges");

            migrationBuilder.DropTable(
                name: "StockMovementDiscrepancies");

            migrationBuilder.DropTable(
                name: "StockMovementStatusHistory");

            migrationBuilder.DropTable(
                name: "StockProcessingPlanStatusHistory");

            migrationBuilder.DropTable(
                name: "StockProcessingResults");

            migrationBuilder.DropTable(
                name: "StockProcessingSessionStatusHistory");

            migrationBuilder.DropTable(
                name: "WithdrawalRequestAgents");

            migrationBuilder.DropTable(
                name: "WithdrawalRequestDenominationBoxes");

            migrationBuilder.DropTable(
                name: "WithdrawalRequestStatusHistory");

            migrationBuilder.DropTable(
                name: "AgentDetails");

            migrationBuilder.DropTable(
                name: "DepositRequestDenominations");

            migrationBuilder.DropTable(
                name: "BagMovements");

            migrationBuilder.DropTable(
                name: "BankingShortages");

            migrationBuilder.DropTable(
                name: "BankingSurpluses");

            migrationBuilder.DropTable(
                name: "FeeTypes");

            migrationBuilder.DropTable(
                name: "DestructionRequests");

            migrationBuilder.DropTable(
                name: "BoardSessionSurplusShortages");

            migrationBuilder.DropTable(
                name: "EodDiscrepancyReports");

            migrationBuilder.DropTable(
                name: "LogisticsCategories");

            migrationBuilder.DropTable(
                name: "MonthlyCommercialBankSettlements");

            migrationBuilder.DropTable(
                name: "MonthlyShortages");

            migrationBuilder.DropTable(
                name: "MonthlySurpluses");

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
                name: "WithdrawalRequestDenominations");

            migrationBuilder.DropTable(
                name: "Bags");

            migrationBuilder.DropTable(
                name: "DestructionAuthorizations");

            migrationBuilder.DropTable(
                name: "DestructionLocations");

            migrationBuilder.DropTable(
                name: "DestructionTypes");

            migrationBuilder.DropTable(
                name: "DepositRequests");

            migrationBuilder.DropTable(
                name: "StockProcessingSessions");

            migrationBuilder.DropTable(
                name: "PackingListRanges");

            migrationBuilder.DropTable(
                name: "ReceivedStatusTypes");

            migrationBuilder.DropTable(
                name: "WithdrawalRequests");

            migrationBuilder.DropTable(
                name: "VaultSections");

            migrationBuilder.DropTable(
                name: "ProcessingBoards");

            migrationBuilder.DropTable(
                name: "StockProcessingPlans");

            migrationBuilder.DropTable(
                name: "ExPrintMintDetails");

            migrationBuilder.DropTable(
                name: "Vaults");

            migrationBuilder.DropTable(
                name: "BPSMachines");

            migrationBuilder.DropTable(
                name: "CountingRooms");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "ExPrintMints");

            migrationBuilder.DropTable(
                name: "Logistics");

            migrationBuilder.DropTable(
                name: "ApprovedCurrencyProcurements");

            migrationBuilder.DropTable(
                name: "CurrencyVendors");

            migrationBuilder.DropTable(
                name: "VendorTypes");
        }
    }
}
