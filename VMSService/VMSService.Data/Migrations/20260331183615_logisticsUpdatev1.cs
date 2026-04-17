using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class logisticsUpdatev1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewStatus",
                table: "LogisticsStatusHistory");

            migrationBuilder.DropColumn(
                name: "OldStatus",
                table: "LogisticsStatusHistory");

            migrationBuilder.AddColumn<int>(
                name: "NewStatusId",
                table: "LogisticsStatusHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OldStatusId",
                table: "LogisticsStatusHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStatusId",
                table: "Logistics",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_LogisticsStatusHistory_NewStatusId",
                table: "LogisticsStatusHistory",
                column: "NewStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_LogisticsStatusHistory_OldStatusId",
                table: "LogisticsStatusHistory",
                column: "OldStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Logistics_CurrentStatusId",
                table: "Logistics",
                column: "CurrentStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Logistics_StatusTypes_CurrentStatusId",
                table: "Logistics",
                column: "CurrentStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LogisticsStatusHistory_StatusTypes_NewStatusId",
                table: "LogisticsStatusHistory",
                column: "NewStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LogisticsStatusHistory_StatusTypes_OldStatusId",
                table: "LogisticsStatusHistory",
                column: "OldStatusId",
                principalTable: "StatusTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Logistics_StatusTypes_CurrentStatusId",
                table: "Logistics");

            migrationBuilder.DropForeignKey(
                name: "FK_LogisticsStatusHistory_StatusTypes_NewStatusId",
                table: "LogisticsStatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_LogisticsStatusHistory_StatusTypes_OldStatusId",
                table: "LogisticsStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_LogisticsStatusHistory_NewStatusId",
                table: "LogisticsStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_LogisticsStatusHistory_OldStatusId",
                table: "LogisticsStatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_Logistics_CurrentStatusId",
                table: "Logistics");

            migrationBuilder.DropColumn(
                name: "NewStatusId",
                table: "LogisticsStatusHistory");

            migrationBuilder.DropColumn(
                name: "OldStatusId",
                table: "LogisticsStatusHistory");

            migrationBuilder.DropColumn(
                name: "CurrentStatusId",
                table: "Logistics");

            migrationBuilder.AddColumn<string>(
                name: "NewStatus",
                table: "LogisticsStatusHistory",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OldStatus",
                table: "LogisticsStatusHistory",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
