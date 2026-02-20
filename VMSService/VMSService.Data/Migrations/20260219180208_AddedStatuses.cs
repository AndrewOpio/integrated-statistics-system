using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentStatusId",
                table: "ExPrintMints",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStatusId",
                table: "ApprovedCurrencyProcurements",
                type: "int",
                nullable: false,
                defaultValue: 0);

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
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ApprovedCurrencyStatusHistory_StatusTypes_NewStatusId",
                        column: x => x.NewStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ApprovedCurrencyStatusHistory_StatusTypes_OldStatusId",
                        column: x => x.OldStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
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
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ExPrintMintStatusHistory_StatusTypes_NewStatusId",
                        column: x => x.NewStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ExPrintMintStatusHistory_StatusTypes_OldStatusId",
                        column: x => x.OldStatusId,
                        principalTable: "StatusTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExPrintMints_CurrentStatusId",
                table: "ExPrintMints",
                column: "CurrentStatusId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovedCurrencyProcurements_StatusTypes_CurrentStatusId",
                table: "ApprovedCurrencyProcurements",
                column: "CurrentStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_ExPrintMints_StatusTypes_CurrentStatusId",
                table: "ExPrintMints",
                column: "CurrentStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovedCurrencyProcurements_StatusTypes_CurrentStatusId",
                table: "ApprovedCurrencyProcurements");

            migrationBuilder.DropForeignKey(
                name: "FK_ExPrintMints_StatusTypes_CurrentStatusId",
                table: "ExPrintMints");

            migrationBuilder.DropTable(
                name: "ApprovedCurrencyStatusHistory");

            migrationBuilder.DropTable(
                name: "ExPrintMintStatusHistory");

            migrationBuilder.DropTable(
                name: "StatusTypes");

            migrationBuilder.DropIndex(
                name: "IX_ExPrintMints_CurrentStatusId",
                table: "ExPrintMints");

            migrationBuilder.DropIndex(
                name: "IX_ApprovedCurrencyProcurements_CurrentStatusId",
                table: "ApprovedCurrencyProcurements");

            migrationBuilder.DropColumn(
                name: "CurrentStatusId",
                table: "ExPrintMints");

            migrationBuilder.DropColumn(
                name: "CurrentStatusId",
                table: "ApprovedCurrencyProcurements");
        }
    }
}
