using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Drops StockTakeDetails columns that turned out unused once the module's UI settled:
    /// LineNumber (display grouping ended up being by Vault Section, not insertion order —
    /// Id serves as an insertion-order fallback if ever needed), SubCategoryId (sub-category
    /// was never collected in the Create form), NumberOfBoxes/NumberOfBundles (the form
    /// captures a counted Amount directly, not box/bundle counts), and Remarks (never exposed
    /// at the line level — only the StockTake header has Remarks).
    /// </remarks>
    public partial class CleanupStockTakeDetailColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DECLARE @constraintName NVARCHAR(200);

                -- Columns created with a DEFAULT clause get an auto-named default constraint
                -- that must be dropped before the column itself can be dropped.
                DECLARE column_cursor CURSOR FOR
                    SELECT dc.name
                    FROM sys.default_constraints dc
                    JOIN sys.columns c ON c.default_object_id = dc.object_id
                    WHERE dc.parent_object_id = OBJECT_ID('StockTakeDetails')
                      AND c.name IN ('LineNumber', 'SubCategoryId', 'NumberOfBoxes', 'NumberOfBundles', 'Remarks');

                OPEN column_cursor;
                FETCH NEXT FROM column_cursor INTO @constraintName;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    EXEC('ALTER TABLE StockTakeDetails DROP CONSTRAINT [' + @constraintName + ']');
                    FETCH NEXT FROM column_cursor INTO @constraintName;
                END
                CLOSE column_cursor;
                DEALLOCATE column_cursor;

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockTakeDetails' AND COLUMN_NAME = 'LineNumber')
                    ALTER TABLE StockTakeDetails DROP COLUMN LineNumber;

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockTakeDetails' AND COLUMN_NAME = 'SubCategoryId')
                    ALTER TABLE StockTakeDetails DROP COLUMN SubCategoryId;

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockTakeDetails' AND COLUMN_NAME = 'NumberOfBoxes')
                    ALTER TABLE StockTakeDetails DROP COLUMN NumberOfBoxes;

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockTakeDetails' AND COLUMN_NAME = 'NumberOfBundles')
                    ALTER TABLE StockTakeDetails DROP COLUMN NumberOfBundles;

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockTakeDetails' AND COLUMN_NAME = 'Remarks')
                    ALTER TABLE StockTakeDetails DROP COLUMN Remarks;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockTakeDetails' AND COLUMN_NAME = 'LineNumber')
                    ALTER TABLE StockTakeDetails ADD LineNumber INT NOT NULL DEFAULT 1;

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockTakeDetails' AND COLUMN_NAME = 'SubCategoryId')
                    ALTER TABLE StockTakeDetails ADD SubCategoryId INT NULL;

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockTakeDetails' AND COLUMN_NAME = 'NumberOfBoxes')
                    ALTER TABLE StockTakeDetails ADD NumberOfBoxes INT NOT NULL DEFAULT 0;

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockTakeDetails' AND COLUMN_NAME = 'NumberOfBundles')
                    ALTER TABLE StockTakeDetails ADD NumberOfBundles INT NULL;

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockTakeDetails' AND COLUMN_NAME = 'Remarks')
                    ALTER TABLE StockTakeDetails ADD Remarks NVARCHAR(MAX) NULL;
            ");
        }
    }
}
