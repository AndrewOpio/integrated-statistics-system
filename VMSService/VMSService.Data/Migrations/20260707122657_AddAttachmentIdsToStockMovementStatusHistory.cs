using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentIdsToStockMovementStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('StockMovementStatusHistory') AND name = 'AttachmentIds')
                    ALTER TABLE StockMovementStatusHistory ADD AttachmentIds NVARCHAR(MAX) NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('StockMovementStatusHistory') AND name = 'AttachmentIds')
                    ALTER TABLE StockMovementStatusHistory DROP COLUMN AttachmentIds;
            ");
        }
    }
}
