using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTakeSystemPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockTakeSystemPositions')
                BEGIN
                    CREATE TABLE StockTakeSystemPositions (
                        Id INT IDENTITY(1,1) NOT NULL,
                        StockTakeId INT NOT NULL,
                        CategoryId INT NOT NULL,
                        DenominationId INT NOT NULL,
                        Value DECIMAL(18,2) NOT NULL DEFAULT 0,
                        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        ModifiedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CreatedBy NVARCHAR(MAX) NOT NULL DEFAULT 'system',
                        ModifiedBy NVARCHAR(MAX) NOT NULL DEFAULT 'system',
                        CONSTRAINT PK_StockTakeSystemPositions PRIMARY KEY (Id),
                        CONSTRAINT FK_StockTakeSystemPositions_StockTakes
                            FOREIGN KEY (StockTakeId) REFERENCES StockTakes(Id)
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockTakeSystemPositions')
                    DROP TABLE StockTakeSystemPositions;
            ");
        }
    }
}
