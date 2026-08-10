using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    public partial class AddReimbursementWorkflow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('MonthlyShortageReimbursements') AND name = 'CurrentStatusId')
                BEGIN
                    ALTER TABLE MonthlyShortageReimbursements ADD CurrentStatusId INT NOT NULL DEFAULT 0;
                END

                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('MonthlyShortageReimbursements') AND name = 'CurrentStatusName')
                BEGIN
                    ALTER TABLE MonthlyShortageReimbursements ADD CurrentStatusName NVARCHAR(100) NOT NULL DEFAULT 'Draft';
                END

                IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('MonthlyShortageReimbursementStatusHistories') AND type = 'U')
                BEGIN
                    CREATE TABLE MonthlyShortageReimbursementStatusHistories (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        MonthlyShortageReimbursementId INT NOT NULL,
                        OldStatusId INT NOT NULL,
                        OldStatusName NVARCHAR(100) NOT NULL,
                        NewStatusId INT NOT NULL,
                        NewStatusName NVARCHAR(100) NOT NULL,
                        ChangedBy NVARCHAR(256) NOT NULL,
                        ChangedAt DATETIME2 NOT NULL,
                        Remarks NVARCHAR(MAX) NULL,
                        AttachmentIds NVARCHAR(MAX) NULL,
                        IsDeleted BIT NOT NULL DEFAULT 0,
                        CreatedBy NVARCHAR(256) NOT NULL DEFAULT '',
                        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        ModifiedBy NVARCHAR(256) NOT NULL DEFAULT '',
                        ModifiedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT FK_MSReimbursementStatusHistories_Reimbursement
                            FOREIGN KEY (MonthlyShortageReimbursementId)
                            REFERENCES MonthlyShortageReimbursements(Id)
                    );
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('MonthlyShortageReimbursementStatusHistories') AND type = 'U')
                    DROP TABLE MonthlyShortageReimbursementStatusHistories;

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('MonthlyShortageReimbursements') AND name = 'CurrentStatusName')
                    ALTER TABLE MonthlyShortageReimbursements DROP COLUMN CurrentStatusName;

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('MonthlyShortageReimbursements') AND name = 'CurrentStatusId')
                    ALTER TABLE MonthlyShortageReimbursements DROP COLUMN CurrentStatusId;
            ");
        }
    }
}
