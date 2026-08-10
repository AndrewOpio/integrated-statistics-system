using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTakeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockTakes')
                BEGIN
                    CREATE TABLE StockTakes (
                        Id INT IDENTITY(1,1) NOT NULL,
                        ReferenceNumber NVARCHAR(MAX) NOT NULL,
                        BranchId INT NOT NULL,
                        CountDate DATETIME2 NOT NULL,
                        CurrentStatusId INT NOT NULL DEFAULT 0,
                        CurrentStatusName NVARCHAR(MAX) NOT NULL DEFAULT 'Draft',
                        Remarks NVARCHAR(MAX) NULL,
                        IsDeleted BIT NOT NULL DEFAULT 0,
                        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        ModifiedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CreatedBy NVARCHAR(MAX) NOT NULL DEFAULT 'system',
                        ModifiedBy NVARCHAR(MAX) NOT NULL DEFAULT 'system',
                        CONSTRAINT PK_StockTakes PRIMARY KEY (Id)
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockTakeDetails')
                BEGIN
                    CREATE TABLE StockTakeDetails (
                        Id INT IDENTITY(1,1) NOT NULL,
                        StockTakeId INT NOT NULL,
                        LineNumber INT NOT NULL DEFAULT 1,
                        CategoryId INT NOT NULL,
                        SubCategoryId INT NULL,
                        DenominationId INT NOT NULL,
                        NumberOfBoxes INT NOT NULL DEFAULT 0,
                        NumberOfBundles INT NULL,
                        NumberOfPieces INT NOT NULL DEFAULT 0,
                        Value DECIMAL(18,2) NOT NULL DEFAULT 0,
                        Remarks NVARCHAR(MAX) NULL,
                        IsDeleted BIT NOT NULL DEFAULT 0,
                        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        ModifiedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CreatedBy NVARCHAR(MAX) NOT NULL DEFAULT 'system',
                        ModifiedBy NVARCHAR(MAX) NOT NULL DEFAULT 'system',
                        CONSTRAINT PK_StockTakeDetails PRIMARY KEY (Id),
                        CONSTRAINT FK_StockTakeDetails_StockTakes
                            FOREIGN KEY (StockTakeId) REFERENCES StockTakes(Id)
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockTakeStatusHistory')
                BEGIN
                    CREATE TABLE StockTakeStatusHistory (
                        Id INT IDENTITY(1,1) NOT NULL,
                        StockTakeId INT NOT NULL,
                        OldStatusId INT NOT NULL,
                        OldStatusName NVARCHAR(MAX) NOT NULL,
                        NewStatusId INT NOT NULL,
                        NewStatusName NVARCHAR(MAX) NOT NULL,
                        ChangedBy NVARCHAR(MAX) NOT NULL,
                        ChangedAt DATETIME2 NOT NULL,
                        Remarks NVARCHAR(MAX) NULL,
                        AttachmentIds NVARCHAR(MAX) NULL,
                        IsDeleted BIT NOT NULL DEFAULT 0,
                        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        ModifiedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CreatedBy NVARCHAR(MAX) NOT NULL DEFAULT 'system',
                        ModifiedBy NVARCHAR(MAX) NOT NULL DEFAULT 'system',
                        CONSTRAINT PK_StockTakeStatusHistory PRIMARY KEY (Id),
                        CONSTRAINT FK_StockTakeStatusHistory_StockTakes
                            FOREIGN KEY (StockTakeId) REFERENCES StockTakes(Id)
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockTakeStatusHistory')
                    DROP TABLE StockTakeStatusHistory;

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockTakeDetails')
                    DROP TABLE StockTakeDetails;

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockTakes')
                    DROP TABLE StockTakes;
            ");
        }
    }
}
