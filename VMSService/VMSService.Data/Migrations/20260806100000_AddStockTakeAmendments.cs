using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTakeAmendments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'StockTakes' AND COLUMN_NAME = 'AmendsStockTakeId'
                )
                BEGIN
                    ALTER TABLE StockTakes ADD AmendsStockTakeId INT NULL;
                END

                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'StockTakes' AND COLUMN_NAME = 'IsLocked'
                )
                BEGIN
                    ALTER TABLE StockTakes ADD IsLocked BIT NOT NULL DEFAULT 0;
                END

                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_StockTakes_StockTakes_AmendsStockTakeId'
                )
                BEGIN
                    ALTER TABLE StockTakes
                        ADD CONSTRAINT FK_StockTakes_StockTakes_AmendsStockTakeId
                        FOREIGN KEY (AmendsStockTakeId) REFERENCES StockTakes(Id);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_StockTakes_StockTakes_AmendsStockTakeId')
                    ALTER TABLE StockTakes DROP CONSTRAINT FK_StockTakes_StockTakes_AmendsStockTakeId;

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockTakes' AND COLUMN_NAME = 'IsLocked')
                    ALTER TABLE StockTakes DROP COLUMN IsLocked;

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockTakes' AND COLUMN_NAME = 'AmendsStockTakeId')
                    ALTER TABLE StockTakes DROP COLUMN AmendsStockTakeId;
            ");
        }
    }
}
