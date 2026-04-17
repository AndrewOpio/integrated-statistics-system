using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class receivedStockTableUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceivedStockListRangeDetails");

            migrationBuilder.DropTable(
                name: "ReceivedStockLists");

            migrationBuilder.DropColumn(
                name: "BoxNumberRangeEnd",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "BoxNumberRangeStart",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "SerialNumberPrefix",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "SerialNumberRangeEnd",
                table: "ReceivedStock");

            migrationBuilder.DropColumn(
                name: "SerialNumberRangeStart",
                table: "ReceivedStock");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "ReceivedStock",
                newName: "TestingFees");

            migrationBuilder.RenameColumn(
                name: "NumberOfPieces",
                table: "ReceivedStock",
                newName: "ReceivedStatusTypeId");

            migrationBuilder.RenameColumn(
                name: "NumberOfBoxes",
                table: "ReceivedStock",
                newName: "ReceivedStatus");

            migrationBuilder.RenameColumn(
                name: "BoxNumberPrefix",
                table: "ReceivedStock",
                newName: "DeliveryAirline");

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

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStock_ReceivedStatusTypeId",
                table: "ReceivedStock",
                column: "ReceivedStatusTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockDetails_PackingListRangeId",
                table: "ReceivedStockDetails",
                column: "PackingListRangeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivedStockDetails_ReceivedStockId",
                table: "ReceivedStockDetails",
                column: "ReceivedStockId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivedStock_ReceivedStatusTypes_ReceivedStatusTypeId",
                table: "ReceivedStock",
                column: "ReceivedStatusTypeId",
                principalTable: "ReceivedStatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReceivedStock_ReceivedStatusTypes_ReceivedStatusTypeId",
                table: "ReceivedStock");

            migrationBuilder.DropTable(
                name: "ReceivedStockDetails");

            migrationBuilder.DropIndex(
                name: "IX_ReceivedStock_ReceivedStatusTypeId",
                table: "ReceivedStock");

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
                name: "ReceivedStatusTypeId",
                table: "ReceivedStock",
                newName: "NumberOfPieces");

            migrationBuilder.RenameColumn(
                name: "ReceivedStatus",
                table: "ReceivedStock",
                newName: "NumberOfBoxes");

            migrationBuilder.RenameColumn(
                name: "DeliveryAirline",
                table: "ReceivedStock",
                newName: "BoxNumberPrefix");

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

            migrationBuilder.AddColumn<int>(
                name: "SerialNumberRangeStart",
                table: "ReceivedStock",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReceivedStockLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackingListRangeId = table.Column<int>(type: "int", nullable: false),
                    ReceivedStatusTypeId = table.Column<int>(type: "int", nullable: false),
                    ClearingFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveryAirline = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HandlingCharges = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InsuranceFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedStatus = table.Column<int>(type: "int", nullable: false),
                    ShippingFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TestingFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
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
                name: "ReceivedStockListRangeDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceivedStockListId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedCaseNumberPrefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedEndCaseNumber = table.Column<int>(type: "int", nullable: false),
                    ReceivedStartCaseNumber = table.Column<int>(type: "int", nullable: false)
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
    }
}
