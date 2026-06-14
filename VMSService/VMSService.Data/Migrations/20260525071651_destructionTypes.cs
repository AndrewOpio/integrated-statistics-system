using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class destructionTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "DestructionRequestDenominations",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.DropTable(
                name: "DestructionRequestWitnesses");

            // Drop the old string DestructionType column and replace with an int FK
            migrationBuilder.DropColumn(
                name: "DestructionType",
                table: "DestructionRequests");

            migrationBuilder.AddColumn<int>(
                name: "DestructionTypeId",
                table: "DestructionRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsWitness",
                table: "DestructionRequestMembers",
                type: "bit",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.CreateIndex(
                name: "IX_DestructionRequests_DestructionTypeId",
                table: "DestructionRequests",
                column: "DestructionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DestructionRequestStatusHistory_DestructionRequestId",
                table: "DestructionRequestStatusHistory",
                column: "DestructionRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_DestructionRequests_DestructionTypes_DestructionTypeId",
                table: "DestructionRequests",
                column: "DestructionTypeId",
                principalTable: "DestructionTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DestructionRequests_DestructionTypes_DestructionTypeId",
                table: "DestructionRequests");

            migrationBuilder.DropTable(
                name: "DestructionRequestStatusHistory");

            migrationBuilder.DropTable(
                name: "DestructionTypes");

            migrationBuilder.DropIndex(
                name: "IX_DestructionRequests_DestructionTypeId",
                table: "DestructionRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Amount",
                table: "DestructionRequestDenominations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.DropColumn(
                name: "DestructionTypeId",
                table: "DestructionRequests");

            migrationBuilder.DropColumn(
                name: "IsWitness",
                table: "DestructionRequestMembers");

            migrationBuilder.AddColumn<string>(
                name: "DestructionType",
                table: "DestructionRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DestructionRequestWitnesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DestructionRequestId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IDNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DestructionRequestWitnesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DestructionRequestWitnesses_DestructionRequests_DestructionRequestId",
                        column: x => x.DestructionRequestId,
                        principalTable: "DestructionRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DestructionRequestWitnesses_DestructionRequestId",
                table: "DestructionRequestWitnesses",
                column: "DestructionRequestId");
        }
    }
}
