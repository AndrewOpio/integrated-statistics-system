using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBankingSurplusColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Drop default constraints then columns for old BankingSurpluses columns
                DECLARE @sql NVARCHAR(MAX) = '';
                SELECT @sql += 'ALTER TABLE BankingSurpluses DROP CONSTRAINT ' + dc.name + '; '
                FROM sys.default_constraints dc
                JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
                WHERE c.object_id = OBJECT_ID('BankingSurpluses')
                  AND c.name IN ('TotalSurplus', 'TotalShortage', 'NetSurplus');
                IF LEN(@sql) > 0 EXEC sp_executesql @sql;

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BankingSurpluses') AND name = 'TotalSurplus')
                    ALTER TABLE BankingSurpluses DROP COLUMN TotalSurplus;
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BankingSurpluses') AND name = 'TotalShortage')
                    ALTER TABLE BankingSurpluses DROP COLUMN TotalShortage;
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BankingSurpluses') AND name = 'NetSurplus')
                    ALTER TABLE BankingSurpluses DROP COLUMN NetSurplus;

                -- Add new BankingSurpluses columns if they don't exist
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BankingSurpluses') AND name = 'BranchId')
                    ALTER TABLE BankingSurpluses ADD BranchId int NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BankingSurpluses') AND name = 'Amount')
                    ALTER TABLE BankingSurpluses ADD Amount decimal(18,2) NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BankingSurpluses') AND name = 'DepositDate')
                    ALTER TABLE BankingSurpluses ADD DepositDate datetime2 NULL;

                -- Drop UserBranchAssignments if it exists
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserBranchAssignments')
                    DROP TABLE UserBranchAssignments;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserBranchAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBranchAssignments", x => x.Id);
                });
        }
    }
}
