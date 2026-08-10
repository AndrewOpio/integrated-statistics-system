using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Backfills Vaults/VaultSections for databases where InitialMigration was recorded as applied
    /// before those tables were added to that migration's source — e.g. this dev DB, where Bags
    /// (a later migration) already exists with no FK to VaultSections, confirming VaultSections
    /// never actually got created here. Schema matches InitialMigration.cs exactly.
    /// </remarks>
    public partial class EnsureVaultTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Vaults')
                BEGIN
                    CREATE TABLE Vaults (
                        Id INT IDENTITY(1,1) NOT NULL,
                        Name NVARCHAR(MAX) NOT NULL,
                        BranchId INT NOT NULL,
                        IsDeleted BIT NOT NULL,
                        IsActive BIT NOT NULL,
                        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        ModifiedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CreatedBy NVARCHAR(MAX) NOT NULL DEFAULT 'system',
                        ModifiedBy NVARCHAR(MAX) NOT NULL DEFAULT 'system',
                        CONSTRAINT PK_Vaults PRIMARY KEY (Id)
                    );
                END

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'VaultSections')
                BEGIN
                    CREATE TABLE VaultSections (
                        Id INT IDENTITY(1,1) NOT NULL,
                        Name NVARCHAR(MAX) NOT NULL,
                        VaultId INT NOT NULL,
                        IsDeleted BIT NOT NULL,
                        IsActive BIT NOT NULL,
                        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        ModifiedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CreatedBy NVARCHAR(MAX) NOT NULL DEFAULT 'system',
                        ModifiedBy NVARCHAR(MAX) NOT NULL DEFAULT 'system',
                        CONSTRAINT PK_VaultSections PRIMARY KEY (Id),
                        CONSTRAINT FK_VaultSections_Vaults_VaultId
                            FOREIGN KEY (VaultId) REFERENCES Vaults(Id)
                    );

                    CREATE INDEX IX_VaultSections_VaultId ON VaultSections (VaultId);
                END

                -- Bags.VaultSectionId already exists (added by AddBagsTable) but was never
                -- constrained since VaultSections didn't exist yet; wire it up now.
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Bags_VaultSections_VaultSectionId')
                BEGIN
                    ALTER TABLE Bags
                        ADD CONSTRAINT FK_Bags_VaultSections_VaultSectionId
                        FOREIGN KEY (VaultSectionId) REFERENCES VaultSections(Id);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Bags_VaultSections_VaultSectionId')
                    ALTER TABLE Bags DROP CONSTRAINT FK_Bags_VaultSections_VaultSectionId;

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'VaultSections')
                    DROP TABLE VaultSections;

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Vaults')
                    DROP TABLE Vaults;
            ");
        }
    }
}
