using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class PenaltyWaiverStockTakeAndPublicHolidayTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "MonthlySurpluses");

            migrationBuilder.DropColumn(
                name: "CommercialBankId",
                table: "MonthlySurpluses");

            migrationBuilder.RenameColumn(
                name: "DepositDate",
                table: "MonthlySurpluses",
                newName: "EftDate");

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

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyShortageReimbursementDenominations_MonthlyShortageReimbursementId",
                table: "MonthlyShortageReimbursementDenominations",
                column: "MonthlyShortageReimbursementId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyShortageReimbursements_MonthlyShortageId",
                table: "MonthlyShortageReimbursements",
                column: "MonthlyShortageId");

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
                name: "IX_StockMovementDenominationBags_BagId",
                table: "StockMovementDenominationBags",
                column: "BagId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovementDenominationBags_StockMovementDenominationId",
                table: "StockMovementDenominationBags",
                column: "StockMovementDenominationId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyShortageReimbursementDenominations");

            migrationBuilder.DropTable(
                name: "MonthlyShortageReimbursementStatusHistories");

            migrationBuilder.DropTable(
                name: "PenaltyWaivers");

            migrationBuilder.DropTable(
                name: "PublicHolidays");

            migrationBuilder.DropTable(
                name: "StockMovementDenominationBags");

            migrationBuilder.DropTable(
                name: "StockTakeDetails");

            migrationBuilder.DropTable(
                name: "StockTakeStatusHistory");

            migrationBuilder.DropTable(
                name: "StockTakeSystemPositions");

            migrationBuilder.DropTable(
                name: "MonthlyShortageReimbursements");

            migrationBuilder.DropTable(
                name: "StockTakes");

            migrationBuilder.RenameColumn(
                name: "EftDate",
                table: "MonthlySurpluses",
                newName: "DepositDate");

            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "MonthlySurpluses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CommercialBankId",
                table: "MonthlySurpluses",
                type: "int",
                nullable: true);
        }
    }
}
