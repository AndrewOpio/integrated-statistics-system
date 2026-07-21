using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBankingShortagesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BankingShortages')
                BEGIN
                    CREATE TABLE BankingShortages (
                        Id              INT IDENTITY(1,1) NOT NULL,
                        ReferenceNumber NVARCHAR(MAX)     NOT NULL,
                        Period          DATETIME2         NOT NULL,
                        TotalSurplus    DECIMAL(18,2)     NOT NULL DEFAULT 0,
                        TotalShortage   DECIMAL(18,2)     NOT NULL DEFAULT 0,
                        NetShortage     DECIMAL(18,2)     NOT NULL DEFAULT 0,
                        Remarks         NVARCHAR(MAX)     NULL,
                        CurrentStatusId   INT             NOT NULL DEFAULT 0,
                        CurrentStatusName NVARCHAR(MAX)   NOT NULL DEFAULT 'Draft',
                        CreatedDate     DATETIME2         NOT NULL DEFAULT GETDATE(),
                        ModifiedDate    DATETIME2         NOT NULL DEFAULT GETDATE(),
                        CreatedBy       NVARCHAR(MAX)     NOT NULL DEFAULT 'system',
                        ModifiedBy      NVARCHAR(MAX)     NOT NULL DEFAULT 'system',
                        CONSTRAINT PK_BankingShortages PRIMARY KEY (Id)
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BankingShortageStatusHistories')
                BEGIN
                    CREATE TABLE BankingShortageStatusHistories (
                        Id                INT IDENTITY(1,1) NOT NULL,
                        BankingShortageId INT               NOT NULL,
                        OldStatusId       INT               NOT NULL DEFAULT 0,
                        OldStatusName     NVARCHAR(MAX)     NOT NULL,
                        NewStatusId       INT               NOT NULL DEFAULT 0,
                        NewStatusName     NVARCHAR(MAX)     NOT NULL,
                        ChangedBy         NVARCHAR(MAX)     NOT NULL,
                        ChangedAt         DATETIME2         NOT NULL,
                        Remarks           NVARCHAR(MAX)     NULL,
                        AttachmentIds     NVARCHAR(MAX)     NULL,
                        IsDeleted         BIT               NOT NULL DEFAULT 0,
                        CreatedDate       DATETIME2         NOT NULL DEFAULT GETDATE(),
                        ModifiedDate      DATETIME2         NOT NULL DEFAULT GETDATE(),
                        CreatedBy         NVARCHAR(MAX)     NOT NULL DEFAULT 'system',
                        ModifiedBy        NVARCHAR(MAX)     NOT NULL DEFAULT 'system',
                        CONSTRAINT PK_BankingShortageStatusHistories PRIMARY KEY (Id),
                        CONSTRAINT FK_BankingShortageStatusHistories_BankingShortages
                            FOREIGN KEY (BankingShortageId) REFERENCES BankingShortages(Id)
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BankingShortageStatusHistories')
                    DROP TABLE BankingShortageStatusHistories;
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BankingShortages')
                    DROP TABLE BankingShortages;
            ");
        }
    }
}
