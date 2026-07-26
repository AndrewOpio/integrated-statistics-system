using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class cfbTablesRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── CfbCashHoldings: replace quantity-based design with versioned amount-based baseline ──

            migrationBuilder.DropIndex(
                name: "IX_CfbCashHoldings_CfbBoxId_DenominationId",
                table: "CfbCashHoldings");

            migrationBuilder.DropColumn(name: "DenomValue", table: "CfbCashHoldings");
            migrationBuilder.DropColumn(name: "Quantity", table: "CfbCashHoldings");
            migrationBuilder.DropColumn(name: "RowVersion", table: "CfbCashHoldings");

            migrationBuilder.AddColumn<long>(
                name: "Amount",
                table: "CfbCashHoldings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CfbCashHoldings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Filtered unique index — exactly one active baseline per (box, denomination).
            migrationBuilder.CreateIndex(
                name: "IX_CfbCashHoldings_Active_Box_Denom",
                table: "CfbCashHoldings",
                columns: new[] { "CfbBoxId", "DenominationId" },
                unique: true,
                filter: "[IsActive] = 1");

            // ── CfbCashMovements: replace quantity-change with signed amount-change ──

            migrationBuilder.DropColumn(name: "DenomValue", table: "CfbCashMovements");
            migrationBuilder.DropColumn(name: "QuantityChange", table: "CfbCashMovements");

            migrationBuilder.AddColumn<long>(
                name: "AmountChange",
                table: "CfbCashMovements",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── CfbCashMovements: restore ────────────────────────────────────────────
            migrationBuilder.DropColumn(name: "AmountChange", table: "CfbCashMovements");

            migrationBuilder.AddColumn<int>(
                name: "QuantityChange",
                table: "CfbCashMovements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DenomValue",
                table: "CfbCashMovements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // ── CfbCashHoldings: restore ─────────────────────────────────────────────
            migrationBuilder.DropIndex(
                name: "IX_CfbCashHoldings_Active_Box_Denom",
                table: "CfbCashHoldings");

            migrationBuilder.DropColumn(name: "IsActive", table: "CfbCashHoldings");
            migrationBuilder.DropColumn(name: "Amount", table: "CfbCashHoldings");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "CfbCashHoldings",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DenomValue",
                table: "CfbCashHoldings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "CfbCashHoldings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CfbCashHoldings_CfbBoxId_DenominationId",
                table: "CfbCashHoldings",
                columns: new[] { "CfbBoxId", "DenominationId" },
                unique: true);
        }
    }
}