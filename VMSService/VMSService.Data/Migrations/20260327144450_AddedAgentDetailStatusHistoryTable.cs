using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedAgentDetailStatusHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommercialBankUsers");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AgentDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AgentDetailStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentDetailId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AgentDetailStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentDetailStatusHistory_AgentDetails_AgentDetailId",
                        column: x => x.AgentDetailId,
                        principalTable: "AgentDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WithdrawalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TraansactionRefNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CbId = table.Column<int>(type: "int", nullable: false),
                    cbUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CbCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InitiatorUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupervisorUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CbBranchId = table.Column<int>(type: "int", nullable: false),
                    BouBranchId = table.Column<int>(type: "int", nullable: false),
                    CollectionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RtgsDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RtgsCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresSpecialApproval = table.Column<bool>(type: "bit", nullable: false),
                    SpecialApprovalReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpecialApprovalBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpecialApprovalAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusTypeId = table.Column<int>(type: "int", nullable: false),
                    TotalNotesAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCoinsAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                        onDelete: ReferentialAction.Cascade);
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
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WithdrawalRequestStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WithdrawalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_WithdrawalRequestStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WithdrawalRequestStatusHistory_WithdrawalRequests_WithdrawalRequestId",
                        column: x => x.WithdrawalRequestId,
                        principalTable: "WithdrawalRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentDetailStatusHistory_AgentDetailId",
                table: "AgentDetailStatusHistory",
                column: "AgentDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequestAgents_WithdrawalRequestId_EnteredBy",
                table: "WithdrawalRequestAgents",
                columns: new[] { "WithdrawalRequestId", "EnteredBy" });

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
                name: "IX_WithdrawalRequests_TraansactionRefNumber",
                table: "WithdrawalRequests",
                column: "TraansactionRefNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WithdrawalRequestStatusHistory_WithdrawalRequestId",
                table: "WithdrawalRequestStatusHistory",
                column: "WithdrawalRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentDetailStatusHistory");

            migrationBuilder.DropTable(
                name: "WithdrawalRequestAgents");

            migrationBuilder.DropTable(
                name: "WithdrawalRequestDenominations");

            migrationBuilder.DropTable(
                name: "WithdrawalRequestStatusHistory");

            migrationBuilder.DropTable(
                name: "WithdrawalRequests");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AgentDetails");

            migrationBuilder.CreateTable(
                name: "CommercialBankUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommercialBankBranchId = table.Column<int>(type: "int", nullable: true),
                    CommercialBankId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommercialBankUsers", x => x.Id);
                });
        }
    }
}
