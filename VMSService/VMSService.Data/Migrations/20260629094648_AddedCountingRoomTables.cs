using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedCountingRoomTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CountingRoomId",
                table: "ProcessingBoards",
                type: "int",
                nullable: false,
                defaultValue: 0);

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
                name: "SessionBags",
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
                    table.PrimaryKey("PK_SessionBags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionBags_Bags_BagId",
                        column: x => x.BagId,
                        principalTable: "Bags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionBags_StockProcessingSessions_StockProcessingSessionId",
                        column: x => x.StockProcessingSessionId,
                        principalTable: "StockProcessingSessions",
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

            migrationBuilder.CreateIndex(
                name: "IX_ProcessingBoards_CountingRoomId",
                table: "ProcessingBoards",
                column: "CountingRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_CountingRoomSessions_CountingRoomId",
                table: "CountingRoomSessions",
                column: "CountingRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_CountingRoomSessions_StockProcessingPlanId",
                table: "CountingRoomSessions",
                column: "StockProcessingPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionBags_BagId",
                table: "SessionBags",
                column: "BagId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionBags_StockProcessingSessionId",
                table: "SessionBags",
                column: "StockProcessingSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessingBoards_CountingRooms_CountingRoomId",
                table: "ProcessingBoards",
                column: "CountingRoomId",
                principalTable: "CountingRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProcessingBoards_CountingRooms_CountingRoomId",
                table: "ProcessingBoards");

            migrationBuilder.DropTable(
                name: "CountingRoomSessions");

            migrationBuilder.DropTable(
                name: "SessionBags");

            migrationBuilder.DropTable(
                name: "CountingRooms");

            migrationBuilder.DropIndex(
                name: "IX_ProcessingBoards_CountingRoomId",
                table: "ProcessingBoards");

            migrationBuilder.DropColumn(
                name: "CountingRoomId",
                table: "ProcessingBoards");
        }
    }
}
