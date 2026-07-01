using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VMSService.Data.Configurations;

#nullable disable

namespace VMSService.Data.Migrations
{
    [DbContext(typeof(VMSServiceDbContext))]
    [Migration("20260701120000_RemoveChildBagsNavigation")]
    public partial class RemoveChildBagsNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the self-referencing BagId FK/index/column from Bags table.
            // Guards handle the case where RemoveParentBagId migration was never applied.
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Bags_Bags_BagId'
      AND parent_object_id = OBJECT_ID('Bags'))
    ALTER TABLE [Bags] DROP CONSTRAINT [FK_Bags_Bags_BagId];

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Bags_BagId'
      AND object_id = OBJECT_ID('Bags'))
    DROP INDEX [IX_Bags_BagId] ON [Bags];

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('Bags')
      AND name = 'BagId')
    ALTER TABLE [Bags] DROP COLUMN [BagId];
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BagId",
                table: "Bags",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bags_BagId",
                table: "Bags",
                column: "BagId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bags_Bags_BagId",
                table: "Bags",
                column: "BagId",
                principalTable: "Bags",
                principalColumn: "Id");
        }
    }
}
