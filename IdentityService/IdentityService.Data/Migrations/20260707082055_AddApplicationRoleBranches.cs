using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationRoleBranches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='ApplicationRoleBranches')
                BEGIN
                    CREATE TABLE ApplicationRoleBranches (
                        Id int IDENTITY(1,1) NOT NULL,
                        ApplicationRoleId int NOT NULL,
                        BranchId int NOT NULL,
                        CreatedDate datetime2 NOT NULL,
                        ModifiedDate datetime2 NOT NULL,
                        CreatedBy nvarchar(max) NOT NULL,
                        ModifiedBy nvarchar(max) NOT NULL,
                        CONSTRAINT PK_ApplicationRoleBranches PRIMARY KEY (Id),
                        CONSTRAINT FK_ApplicationRoleBranches_ApplicationRoles_ApplicationRoleId
                            FOREIGN KEY (ApplicationRoleId) REFERENCES ApplicationRoles (Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IX_ApplicationRoleBranches_ApplicationRoleId
                        ON ApplicationRoleBranches (ApplicationRoleId);
                END

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='CommercialBankUsers')
                BEGIN
                    CREATE TABLE CommercialBankUsers (
                        Id int IDENTITY(1,1) NOT NULL,
                        UserId nvarchar(450) NOT NULL,
                        CommercialBankId int NOT NULL,
                        CommercialBankBranchId int NULL,
                        IsDeleted bit NOT NULL,
                        CreatedDate datetime2 NOT NULL,
                        ModifiedDate datetime2 NOT NULL,
                        CreatedBy nvarchar(max) NOT NULL,
                        ModifiedBy nvarchar(max) NOT NULL,
                        CONSTRAINT PK_CommercialBankUsers PRIMARY KEY (Id),
                        CONSTRAINT FK_CommercialBankUsers_AspNetUsers_UserId
                            FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IX_CommercialBankUsers_UserId ON CommercialBankUsers (UserId);
                END

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='CommercialBankUsers' AND COLUMN_NAME='CommercialBankBranchId')
                    ALTER TABLE CommercialBankUsers ADD CommercialBankBranchId int NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationRoleBranches");

            migrationBuilder.DropTable(
                name: "CommercialBankUsers");
        }
    }
}
