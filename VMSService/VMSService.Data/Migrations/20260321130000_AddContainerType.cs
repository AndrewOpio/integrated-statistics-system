using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContainerType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'StockMovementDenominations' 
                    AND COLUMN_NAME = 'ContainerType'
                )
                BEGIN
                    ALTER TABLE StockMovementDenominations 
                    ADD ContainerType NVARCHAR(50) NULL
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'StockMovementDenominations' 
                    AND COLUMN_NAME = 'ContainerType'
                )
                BEGIN
                    ALTER TABLE StockMovementDenominations 
                    DROP COLUMN ContainerType
                END
            ");
        }
    }
}
