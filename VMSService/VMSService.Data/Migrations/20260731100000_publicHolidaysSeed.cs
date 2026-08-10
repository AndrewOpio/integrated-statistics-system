using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class publicHolidaysSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 2026 Uganda public holidays (fixed calendar dates only) ─────────────
            // Movable feast days (Good Friday, Easter Monday, Eid al-Fitr, Eid al-Adha)
            // are NOT seeded here — their 2026 dates must be confirmed against an
            // authoritative calendar and added via the Public Holidays admin screen.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [PublicHolidays] WHERE [Date] = '2026-01-01')
INSERT INTO [PublicHolidays] ([Date], [Name], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES ('2026-01-01', N'New Year''s Day', 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [PublicHolidays] WHERE [Date] = '2026-01-26')
INSERT INTO [PublicHolidays] ([Date], [Name], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES ('2026-01-26', N'NRM Liberation Day', 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [PublicHolidays] WHERE [Date] = '2026-03-08')
INSERT INTO [PublicHolidays] ([Date], [Name], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES ('2026-03-08', N'International Women''s Day', 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [PublicHolidays] WHERE [Date] = '2026-05-01')
INSERT INTO [PublicHolidays] ([Date], [Name], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES ('2026-05-01', N'Labour Day', 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [PublicHolidays] WHERE [Date] = '2026-06-03')
INSERT INTO [PublicHolidays] ([Date], [Name], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES ('2026-06-03', N'Martyrs'' Day', 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [PublicHolidays] WHERE [Date] = '2026-06-09')
INSERT INTO [PublicHolidays] ([Date], [Name], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES ('2026-06-09', N'Heroes'' Day', 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [PublicHolidays] WHERE [Date] = '2026-10-09')
INSERT INTO [PublicHolidays] ([Date], [Name], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES ('2026-10-09', N'Independence Day', 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [PublicHolidays] WHERE [Date] = '2026-12-25')
INSERT INTO [PublicHolidays] ([Date], [Name], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES ('2026-12-25', N'Christmas Day', 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [PublicHolidays] WHERE [Date] = '2026-12-26')
INSERT INTO [PublicHolidays] ([Date], [Name], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES ('2026-12-26', N'Boxing Day', 1, GETDATE(), GETDATE(), N'system', N'system');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM [PublicHolidays] WHERE [Date] IN (
    '2026-01-01', '2026-01-26', '2026-03-08', '2026-05-01', '2026-06-03',
    '2026-06-09', '2026-10-09', '2026-12-25', '2026-12-26'
);");
        }
    }
}
