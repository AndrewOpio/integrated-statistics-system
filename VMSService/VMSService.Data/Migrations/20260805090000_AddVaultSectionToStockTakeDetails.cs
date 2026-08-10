using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVaultSectionToStockTakeDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'StockTakeDetails' AND COLUMN_NAME = 'VaultSectionId'
                )
                BEGIN
                    ALTER TABLE StockTakeDetails ADD VaultSectionId INT NOT NULL DEFAULT 0;
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_StockTakeDetails_VaultSections'
                )
                BEGIN
                    ALTER TABLE StockTakeDetails
                        ADD CONSTRAINT FK_StockTakeDetails_VaultSections
                        FOREIGN KEY (VaultSectionId) REFERENCES VaultSections(Id);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_StockTakeDetails_VaultSections')
                    ALTER TABLE StockTakeDetails DROP CONSTRAINT FK_StockTakeDetails_VaultSections;

                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'StockTakeDetails' AND COLUMN_NAME = 'VaultSectionId'
                )
                    ALTER TABLE StockTakeDetails DROP COLUMN VaultSectionId;
            ");
        }
    }
}
