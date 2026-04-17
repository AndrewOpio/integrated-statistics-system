using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class serialNumberRanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "PackingListRanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseNumberPrefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartCaseNumber = table.Column<int>(type: "int", nullable: false),
                    EndCaseNumber = table.Column<int>(type: "int", nullable: false),
                    SNPrefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SNRangeStart = table.Column<int>(type: "int", nullable: false),
                    SNRangeEnd = table.Column<int>(type: "int", nullable: false),
                    ExStockId = table.Column<int>(type: "int", nullable: false),
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
                        name: "FK_PackingListRanges_ExPrintMints_ExpectedStockId",
                        column: x => x.ExpectedStockId,
                        principalTable: "ExPrintMints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "PackingListDetails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackingListRangeId = table.Column<long>(type: "bigint", nullable: false),
                    CaseNumber = table.Column<int>(type: "int", nullable: false),
                    SerialNoteStartNumber = table.Column<int>(type: "int", nullable: false),
                    SerialNoteEndNumber = table.Column<int>(type: "int", nullable: false),
                    PackingListRangeId1 = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackingListDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackingListDetails_PackingListRanges_PackingListRangeId1",
                        column: x => x.PackingListRangeId1,
                        principalTable: "PackingListRanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceivedStockLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackingListRangeId = table.Column<int>(type: "int", nullable: false),
                    ReceivedStatus = table.Column<int>(type: "int", nullable: false),
                    DeliveryAirline = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TestingFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ShippingFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InsuranceFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HandlingCharges = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClearingFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceivedStatusTypeId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivedStockLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceivedStockLists_PackingListRanges_PackingListRangeId",
                        column: x => x.PackingListRangeId,
                        principalTable: "PackingListRanges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReceivedStockLists_ReceivedStatusTypes_ReceivedStatusTypeId",
                        column: x => x.ReceivedStatusTypeId,
                        principalTable: "ReceivedStatusTypes",
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

            migrationBuilder.CreateTable(
                name: "ReceivedStockListRangeDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceivedStockListId = table.Column<int>(type: "int", nullable: false),
                    ReceivedCaseNumberPrefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedStartCaseNumber = table.Column<int>(type: "int", nullable: false),
                    ReceivedEndCaseNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivedStockListRangeDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceivedStockListRangeDetails_ReceivedStockLists_ReceivedStockListId",
                        column: x => x.ReceivedStockListId,
                        principalTable: "ReceivedStockLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_PackingListDetails_PackingListRangeId1",
                table: "PackingListDetails",
                column: "PackingListRangeId1");

            migrationBuilder.CreateIndex(
                name: "IX_PackingListRanges_ExpectedStockId",
                table: "PackingListRanges",
                column: "ExpectedStockId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockListRangeDetails_ReceivedStockListId",
                table: "ReceivedStockListRangeDetails",
                column: "ReceivedStockListId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockLists_PackingListRangeId",
                table: "ReceivedStockLists",
                column: "PackingListRangeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockLists_ReceivedStatusTypeId",
                table: "ReceivedStockLists",
                column: "ReceivedStatusTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewStockMovementLogs");

            migrationBuilder.DropTable(
                name: "ReceivedStockListRangeDetails");

            migrationBuilder.DropTable(
                name: "NewCaseFullStatusTypes");

            migrationBuilder.DropTable(
                name: "NewStockAccountingStatuses");

            migrationBuilder.DropTable(
                name: "PackingListDetails");

            migrationBuilder.DropTable(
                name: "ReceivedStockLists");

            migrationBuilder.DropTable(
                name: "PackingListRanges");

            migrationBuilder.DropTable(
                name: "ReceivedStatusTypes");
        }
    }
}
