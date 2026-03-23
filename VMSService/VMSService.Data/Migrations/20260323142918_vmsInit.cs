using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class vmsInit : Migration
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
                name: "CommercialBankUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommercialBankId = table.Column<int>(type: "int", nullable: false),
                    CommercialBankBranchId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercialBankUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Logistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatusTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusTypes", x => x.Id);
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
                name: "LogisticsCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogisticsId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogisticsCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogisticsCategories_Logistics_LogisticsId",
                        column: x => x.LogisticsId,
                        principalTable: "Logistics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovedCurrencyProcurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovedCurrencyProcurements_StatusTypes_CurrentStatusId",
                        column: x => x.CurrentStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    DispatchingBranchId = table.Column<int>(type: "int", nullable: true),
                    ReceivingBranchId = table.Column<int>(type: "int", nullable: true),
                    Categorization = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                        name: "FK_StockMovements_StatusTypes_CurrentStatusId",
                        column: x => x.CurrentStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTransitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromStatusId = table.Column<int>(type: "int", nullable: false),
                    ToStatusId = table.Column<int>(type: "int", nullable: false),
                    ActionName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiresComments = table.Column<bool>(type: "bit", nullable: false),
                    DisplayLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ButtonClass = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_StatusTypes_FromStatusId",
                        column: x => x.FromStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowTransitions_StatusTypes_ToStatusId",
                        column: x => x.ToStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogisticsDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogisticsCategoryId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogisticsDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogisticsDetails_LogisticsCategories_LogisticsCategoryId",
                        column: x => x.LogisticsCategoryId,
                        principalTable: "LogisticsCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovedCurrencyStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovedCurrencyProcurementId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ApprovedCurrencyStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovedCurrencyStatusHistory_ApprovedCurrencyProcurements_ApprovedCurrencyProcurementId",
                        column: x => x.ApprovedCurrencyProcurementId,
                        principalTable: "ApprovedCurrencyProcurements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovedCurrencyStatusHistory_StatusTypes_NewStatusId",
                        column: x => x.NewStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovedCurrencyStatusHistory_StatusTypes_OldStatusId",
                        column: x => x.OldStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockMovementDiscrepancies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockMovementId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovementDiscrepancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovementDiscrepancies_StatusTypes_CurrentStatusId",
                        column: x => x.CurrentStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockMovementDiscrepancies_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockMovementStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockMovementId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_StockMovementStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovementStatusHistory_StatusTypes_NewStatusId",
                        column: x => x.NewStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMovementStatusHistory_StatusTypes_OldStatusId",
                        column: x => x.OldStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StockMovementStatusHistory_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovedCurrencyDenominations_CurrencyVendors_CurrencyVendorId",
                        column: x => x.CurrencyVendorId,
                        principalTable: "CurrencyVendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    NumberOfPieces = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BoxNumberPrefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BoxNumberRangeStart = table.Column<int>(type: "int", nullable: true),
                    BoxNumberRangeEnd = table.Column<int>(type: "int", nullable: true),
                    SerialNumberPrefix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerialNumberRangeStart = table.Column<int>(type: "int", nullable: true),
                    SerialNumberRangeEnd = table.Column<int>(type: "int", nullable: true),
                    Attachment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedCurrencyProcurementId = table.Column<int>(type: "int", nullable: false),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    NumberOfBoxes = table.Column<int>(type: "int", nullable: false),
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
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExPrintMints_CurrencyVendors_CurrencyVendorId",
                        column: x => x.CurrencyVendorId,
                        principalTable: "CurrencyVendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExPrintMints_StatusTypes_CurrentStatusId",
                        column: x => x.CurrentStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockMovementDenominationSerialNumberRanges",
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
                    table.PrimaryKey("PK_StockMovementDenominationSerialNumberRanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovementDenominationSerialNumberRanges_StockMovementDenominations_StockMovementDenominationId",
                        column: x => x.StockMovementDenominationId,
                        principalTable: "StockMovementDenominations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExPrintMintStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExPrintMintId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ExPrintMintStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExPrintMintStatusHistory_ExPrintMints_ExPrintMintId",
                        column: x => x.ExPrintMintId,
                        principalTable: "ExPrintMints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExPrintMintStatusHistory_StatusTypes_NewStatusId",
                        column: x => x.NewStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExPrintMintStatusHistory_StatusTypes_OldStatusId",
                        column: x => x.OldStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceivedStock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExPrintMintId = table.Column<int>(type: "int", nullable: false),
                    DateDelivered = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NumberOfPieces = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BoxNumberPrefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BoxNumberRangeStart = table.Column<int>(type: "int", nullable: true),
                    BoxNumberRangeEnd = table.Column<int>(type: "int", nullable: true),
                    SerialNumberPrefix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerialNumberRangeStart = table.Column<int>(type: "int", nullable: true),
                    SerialNumberRangeEnd = table.Column<int>(type: "int", nullable: true),
                    CurrentStatusId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsPartial = table.Column<bool>(type: "bit", nullable: false),
                    NumberOfBoxes = table.Column<int>(type: "int", nullable: false),
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
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReceivedStock_StatusTypes_CurrentStatusId",
                        column: x => x.CurrentStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceivedStockStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceivedStockId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ReceivedStockStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceivedStockStatusHistory_ReceivedStock_ReceivedStockId",
                        column: x => x.ReceivedStockId,
                        principalTable: "ReceivedStock",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReceivedStockStatusHistory_StatusTypes_NewStatusId",
                        column: x => x.NewStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReceivedStockStatusHistory_StatusTypes_OldStatusId",
                        column: x => x.OldStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedCurrencyDenominations_ApprovedCurrencyId",
                table: "ApprovedCurrencyDenominations",
                column: "ApprovedCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedCurrencyDenominations_CurrencyVendorId",
                table: "ApprovedCurrencyDenominations",
                column: "CurrencyVendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedCurrencyProcurements_CurrentStatusId",
                table: "ApprovedCurrencyProcurements",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedCurrencyStatusHistory_ApprovedCurrencyProcurementId",
                table: "ApprovedCurrencyStatusHistory",
                column: "ApprovedCurrencyProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedCurrencyStatusHistory_NewStatusId",
                table: "ApprovedCurrencyStatusHistory",
                column: "NewStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedCurrencyStatusHistory_OldStatusId",
                table: "ApprovedCurrencyStatusHistory",
                column: "OldStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyVendors_VendorTypeId",
                table: "CurrencyVendors",
                column: "VendorTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMints_ApprovedCurrencyProcurementId",
                table: "ExPrintMints",
                column: "ApprovedCurrencyProcurementId");

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMints_CurrencyVendorId",
                table: "ExPrintMints",
                column: "CurrencyVendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMints_CurrentStatusId",
                table: "ExPrintMints",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMintStatusHistory_ExPrintMintId",
                table: "ExPrintMintStatusHistory",
                column: "ExPrintMintId");

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMintStatusHistory_NewStatusId",
                table: "ExPrintMintStatusHistory",
                column: "NewStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMintStatusHistory_OldStatusId",
                table: "ExPrintMintStatusHistory",
                column: "OldStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_LogisticsCategories_LogisticsId",
                table: "LogisticsCategories",
                column: "LogisticsId");

            migrationBuilder.CreateIndex(
                name: "IX_LogisticsDetails_LogisticsCategoryId",
                table: "LogisticsDetails",
                column: "LogisticsCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStock_CurrentStatusId",
                table: "ReceivedStock",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStock_ExPrintMintId",
                table: "ReceivedStock",
                column: "ExPrintMintId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockStatusHistory_NewStatusId",
                table: "ReceivedStockStatusHistory",
                column: "NewStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockStatusHistory_OldStatusId",
                table: "ReceivedStockStatusHistory",
                column: "OldStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockStatusHistory_ReceivedStockId",
                table: "ReceivedStockStatusHistory",
                column: "ReceivedStockId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementDenominations_StockMovementId",
                table: "StockMovementDenominations",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementDenominationSerialNumberRanges_StockMovementDenominationId",
                table: "StockMovementDenominationSerialNumberRanges",
                column: "StockMovementDenominationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementDiscrepancies_CurrentStatusId",
                table: "StockMovementDiscrepancies",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementDiscrepancies_StockMovementId",
                table: "StockMovementDiscrepancies",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_CurrentStatusId",
                table: "StockMovements",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementStatusHistory_NewStatusId",
                table: "StockMovementStatusHistory",
                column: "NewStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementStatusHistory_OldStatusId",
                table: "StockMovementStatusHistory",
                column: "OldStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementStatusHistory_StockMovementId",
                table: "StockMovementStatusHistory",
                column: "StockMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_FromStatusId",
                table: "WorkflowTransitions",
                column: "FromStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransitions_ToStatusId",
                table: "WorkflowTransitions",
                column: "ToStatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentDetails");

            migrationBuilder.DropTable(
                name: "ApprovedCurrencyDenominations");

            migrationBuilder.DropTable(
                name: "ApprovedCurrencyStatusHistory");

            migrationBuilder.DropTable(
                name: "CommercialBankUsers");

            migrationBuilder.DropTable(
                name: "ExPrintMintStatusHistory");

            migrationBuilder.DropTable(
                name: "LogisticsDetails");

            migrationBuilder.DropTable(
                name: "ReceivedStockStatusHistory");

            migrationBuilder.DropTable(
                name: "StockMovementDenominationSerialNumberRanges");

            migrationBuilder.DropTable(
                name: "StockMovementDiscrepancies");

            migrationBuilder.DropTable(
                name: "StockMovementStatusHistory");

            migrationBuilder.DropTable(
                name: "WorkflowTransitions");

            migrationBuilder.DropTable(
                name: "LogisticsCategories");

            migrationBuilder.DropTable(
                name: "ReceivedStock");

            migrationBuilder.DropTable(
                name: "StockMovementDenominations");

            migrationBuilder.DropTable(
                name: "Logistics");

            migrationBuilder.DropTable(
                name: "ExPrintMints");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "ApprovedCurrencyProcurements");

            migrationBuilder.DropTable(
                name: "CurrencyVendors");

            migrationBuilder.DropTable(
                name: "StatusTypes");

            migrationBuilder.DropTable(
                name: "VendorTypes");
        }
    }
}
