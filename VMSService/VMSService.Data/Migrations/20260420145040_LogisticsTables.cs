using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class LogisticsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockMovementDenominationSerialNumberRanges");

            migrationBuilder.DropColumn(
                name: "Categorization",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "BoxNumberRangeEnd",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "BoxNumberRangeStart",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "NumberOfPieces",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "SerialNumberPrefix",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "SerialNumberRangeEnd",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "BoxNumberPrefix",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "BoxNumberRangeEnd",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "BoxNumberRangeStart",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "NumberOfBoxes",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "NumberOfPieces",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "SerialNumberPrefix",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "SerialNumberRangeEnd",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "SerialNumberRangeStart",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "ExPrintMints");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "ReceivedStock",
                newName: "TestingFees");

            migrationBuilder.RenameColumn(
                name: "SerialNumberRangeStart",
                table: "ReceivedStock",
                newName: "ReceivedStatusTypesId");

            migrationBuilder.RenameColumn(
                name: "BoxNumberPrefix",
                table: "ReceivedStock",
                newName: "DeliveryAirline");

            migrationBuilder.AddColumn<int>(
                name: "CommercialBankBranchId",
                table: "StockMovements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LogisticsId",
                table: "StockMovements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Categorization",
                table: "StockMovementDenominations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContainerType",
                table: "StockMovementDenominations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsPartial",
                table: "ReceivedStock",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<decimal>(
                name: "ClearingFees",
                table: "ReceivedStock",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HandlingCharges",
                table: "ReceivedStock",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "InsuranceFees",
                table: "ReceivedStock",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingFees",
                table: "ReceivedStock",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

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
                        onDelete: ReferentialAction.Cascade);
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
                        onDelete: ReferentialAction.Cascade);
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
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_PackingListRanges_ExPrintMints_ExpectedStockId",
                        column: x => x.ExpectedStockId,
                        principalTable: "ExPrintMints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
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
                        onDelete: ReferentialAction.Cascade);
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
                        onDelete: ReferentialAction.Cascade);
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
                        onDelete: ReferentialAction.Cascade);
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
                    NewStockAccountingStatus = table.Column<int>(type: "int", nullable: false),
                    NewCaseFullStatus = table.Column<int>(type: "int", nullable: false),
                    AccountingStatusId = table.Column<int>(type: "int", nullable: false),
                    CaseFullStatusId = table.Column<int>(type: "int", nullable: false),
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
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NewStockMovementLogs_NewStockAccountingStatuses_AccountingStatusId",
                        column: x => x.AccountingStatusId,
                        principalTable: "NewStockAccountingStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NewStockMovementLogs_PackingListDetails_PackingListDetailId",
                        column: x => x.PackingListDetailId,
                        principalTable: "PackingListDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NewStockMovementLogs_StockMovements_StockMovementId",
                        column: x => x.StockMovementId,
                        principalTable: "StockMovements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_LogisticsId",
                table: "StockMovements",
                column: "LogisticsId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStock_ReceivedStatusTypesId",
                table: "ReceivedStock",
                column: "ReceivedStatusTypesId");

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMintDetails_ExPrintMintId",
                table: "ExPrintMintDetails",
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
                name: "IX_ReceivedStockDetails_PackingListRangeId",
                table: "ReceivedStockDetails",
                column: "PackingListRangeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockDetails_ReceivedStockId",
                table: "ReceivedStockDetails",
                column: "ReceivedStockId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementDenominationBoxNumberRanges_StockMovementDenominationId",
                table: "StockMovementDenominationBoxNumberRanges",
                column: "StockMovementDenominationId");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_ReceivedStock_ReceivedStatusTypes_ReceivedStatusTypesId",
            //    table: "ReceivedStock",
            //    column: "ReceivedStatusTypesId",
            //    principalTable: "ReceivedStatusTypes",
            //    principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Logistics_LogisticsId",
                table: "StockMovements",
                column: "LogisticsId",
                principalTable: "Logistics",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_ReceivedStock_ReceivedStatusTypes_ReceivedStatusTypesId",
            //    table: "ReceivedStock");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Logistics_LogisticsId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "LogisticsDetails");

            migrationBuilder.DropTable(
                name: "LogisticsStatusHistory");

            migrationBuilder.DropTable(
                name: "NewStockMovementLogs");

            migrationBuilder.DropTable(
                name: "ReceivedStatusTypes");

            migrationBuilder.DropTable(
                name: "ReceivedStockDetails");

            migrationBuilder.DropTable(
                name: "StockMovementDenominationBoxNumberRanges");

            migrationBuilder.DropTable(
                name: "LogisticsCategories");

            migrationBuilder.DropTable(
                name: "NewCaseFullStatusTypes");

            migrationBuilder.DropTable(
                name: "NewStockAccountingStatuses");

            migrationBuilder.DropTable(
                name: "PackingListDetails");

            migrationBuilder.DropTable(
                name: "Logistics");

            migrationBuilder.DropTable(
                name: "PackingListRanges");

            migrationBuilder.DropTable(
                name: "ExPrintMintDetails");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_LogisticsId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_ReceivedStock_ReceivedStatusTypesId",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "CommercialBankBranchId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "LogisticsId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "Categorization",
                table: "StockMovementDenominations");

            migrationBuilder.DropColumn(
                name: "ContainerType",
                table: "StockMovementDenominations");

            migrationBuilder.DropColumn(
                name: "ClearingFees",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "HandlingCharges",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "InsuranceFees",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "ShippingFees",
                table: "ReceivedStock");

            migrationBuilder.RenameColumn(
                name: "TestingFees",
                table: "ReceivedStock",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "ReceivedStatusTypesId",
                table: "ReceivedStock",
                newName: "SerialNumberRangeStart");

            migrationBuilder.RenameColumn(
                name: "DeliveryAirline",
                table: "ReceivedStock",
                newName: "BoxNumberPrefix");

            migrationBuilder.AddColumn<string>(
                name: "Categorization",
                table: "StockMovements",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsPartial",
                table: "ReceivedStock",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BoxNumberRangeEnd",
                table: "ReceivedStock",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BoxNumberRangeStart",
                table: "ReceivedStock",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfPieces",
                table: "ReceivedStock",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumberPrefix",
                table: "ReceivedStock",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SerialNumberRangeEnd",
                table: "ReceivedStock",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BoxNumberPrefix",
                table: "ExPrintMints",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "BoxNumberRangeEnd",
                table: "ExPrintMints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BoxNumberRangeStart",
                table: "ExPrintMints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfBoxes",
                table: "ExPrintMints",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfPieces",
                table: "ExPrintMints",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumberPrefix",
                table: "ExPrintMints",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SerialNumberRangeEnd",
                table: "ExPrintMints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SerialNumberRangeStart",
                table: "ExPrintMints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Value",
                table: "ExPrintMints",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "StockMovementDenominationSerialNumberRanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockMovementDenominationId = table.Column<int>(type: "int", nullable: false),
                    End = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Start = table.Column<string>(type: "nvarchar(max)", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementDenominationSerialNumberRanges_StockMovementDenominationId",
                table: "StockMovementDenominationSerialNumberRanges",
                column: "StockMovementDenominationId");
        }
    }
}
