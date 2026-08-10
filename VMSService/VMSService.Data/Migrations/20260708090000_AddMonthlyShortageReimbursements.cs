using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyShortageReimbursements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MonthlyShortageReimbursements')
                BEGIN
                    CREATE TABLE MonthlyShortageReimbursements (
                        Id INT IDENTITY(1,1) NOT NULL,
                        ReferenceNumber NVARCHAR(MAX) NOT NULL,
                        MonthlyShortageId INT NOT NULL,
                        BranchId INT NOT NULL,
                        TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                        Remarks NVARCHAR(MAX) NULL,
                        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        ModifiedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CreatedBy NVARCHAR(MAX) NOT NULL DEFAULT 'system',
                        ModifiedBy NVARCHAR(MAX) NOT NULL DEFAULT 'system',
                        CONSTRAINT PK_MonthlyShortageReimbursements PRIMARY KEY (Id),
                        CONSTRAINT FK_MonthlyShortageReimbursements_MonthlyShortages
                            FOREIGN KEY (MonthlyShortageId) REFERENCES MonthlyShortages(Id)
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MonthlyShortageReimbursementDenominations')
                BEGIN
                    CREATE TABLE MonthlyShortageReimbursementDenominations (
                        Id INT IDENTITY(1,1) NOT NULL,
                        MonthlyShortageReimbursementId INT NOT NULL,
                        DenominationId INT NOT NULL,
                        DenominationName NVARCHAR(MAX) NOT NULL,
                        DenominationValue DECIMAL(18,2) NOT NULL,
                        Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
                        NumberOfPieces INT NOT NULL DEFAULT 0,
                        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        ModifiedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CreatedBy NVARCHAR(MAX) NOT NULL DEFAULT 'system',
                        ModifiedBy NVARCHAR(MAX) NOT NULL DEFAULT 'system',
                        CONSTRAINT PK_MonthlyShortageReimbursementDenominations PRIMARY KEY (Id),
                        CONSTRAINT FK_MonthlyShortageReimbursementDenominations_Reimbursements
                            FOREIGN KEY (MonthlyShortageReimbursementId)
                            REFERENCES MonthlyShortageReimbursements(Id)
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MonthlyShortageReimbursementDenominations')
                    DROP TABLE MonthlyShortageReimbursementDenominations;

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MonthlyShortageReimbursements')
                    DROP TABLE MonthlyShortageReimbursements;
            ");
        }
    }
}
