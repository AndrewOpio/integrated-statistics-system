using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class penaltiesSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Pre-Transaction ─────────────────────
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Penalties] WHERE [Name] = N'Late submission of letter of intent to transact')
INSERT INTO [Penalties] ([Name], [Description], [AppliesTo], [Unit], [CutOffTime], [PenaltyValue], [PenaltyPercent], [PercentCutOff], [PenaltyMultiplier], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES (N'Late submission of letter of intent to transact', N'Late submission of the letter of intent to transact — received after 11:59pm of the previous day (by 11:00am on a Monday/public holiday).', N'Pre-Transaction', NULL, '23:59:00', 500000, NULL, NULL, NULL, 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Penalties] WHERE [Name] = N'Late submission of RTGS to transact')
INSERT INTO [Penalties] ([Name], [Description], [AppliesTo], [Unit], [CutOffTime], [PenaltyValue], [PenaltyPercent], [PercentCutOff], [PenaltyMultiplier], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES (N'Late submission of RTGS to transact', N'Late submission of the RTGS to transact — received after 11:00am the same day.', N'Pre-Transaction', NULL, '11:00:00', 500000, NULL, NULL, NULL, 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Penalties] WHERE [Name] = N'Transacting late (after 3:00pm)')
INSERT INTO [Penalties] ([Name], [Description], [AppliesTo], [Unit], [CutOffTime], [PenaltyValue], [PenaltyPercent], [PercentCutOff], [PenaltyMultiplier], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES (N'Transacting late (after 3:00pm)', N'Transacting late with a deposit/withdrawal (i.e. after 3:00pm).', N'Pre-Transaction', NULL, '15:00:00', 1000000, NULL, NULL, NULL, 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Penalties] WHERE [Name] = N'Failure to cancel in time')
INSERT INTO [Penalties] ([Name], [Description], [AppliesTo], [Unit], [CutOffTime], [PenaltyValue], [PenaltyPercent], [PercentCutOff], [PenaltyMultiplier], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES (N'Failure to cancel in time', N'Failure to cancel in time an intention to transact after submission of the letter of intent. Cancellation before 3:00pm attracts no charge; after 3:00pm a charge applies.', N'Pre-Transaction', NULL, '15:00:00', 500000, NULL, NULL, NULL, 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Penalties] WHERE [Name] = N'Failure to amend in time')
INSERT INTO [Penalties] ([Name], [Description], [AppliesTo], [Unit], [CutOffTime], [PenaltyValue], [PenaltyPercent], [PercentCutOff], [PenaltyMultiplier], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES (N'Failure to amend in time', N'Failure to amend in time an intention to transact after submission of the letter of intent. Amendment before 11:00am attracts no charge; after 11:00am a charge applies.', N'Pre-Transaction', NULL, '11:00:00', 500000, NULL, NULL, NULL, 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Penalties] WHERE [Name] = N'Use of unauthorised personnel/agents and CIT')
INSERT INTO [Penalties] ([Name], [Description], [AppliesTo], [Unit], [CutOffTime], [PenaltyValue], [PenaltyPercent], [PercentCutOff], [PenaltyMultiplier], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES (N'Use of unauthorised personnel/agents and CIT', N'Use of unauthorised personnel/agents and CIT plus its personnel.', N'Pre-Transaction', NULL, NULL, 500000, NULL, NULL, NULL, 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Penalties] WHERE [Name] = N'Error on the letter of intent and RTGS')
INSERT INTO [Penalties] ([Name], [Description], [AppliesTo], [Unit], [CutOffTime], [PenaltyValue], [PenaltyPercent], [PercentCutOff], [PenaltyMultiplier], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES (N'Error on the letter of intent and RTGS', N'Error on the letter of intent and RTGS.', N'Pre-Transaction', NULL, NULL, 100000, NULL, NULL, NULL, 1, GETDATE(), GETDATE(), N'system', N'system');");

            // ── At the Point of Transaction ─────────────────────
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Penalties] WHERE [Name] = N'Mismatch of value/category vs physical count')
INSERT INTO [Penalties] ([Name], [Description], [AppliesTo], [Unit], [CutOffTime], [PenaltyValue], [PenaltyPercent], [PercentCutOff], [PenaltyMultiplier], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES (N'Mismatch of value/category vs physical count', N'Mismatch between the value/category to be deposited (as declared in the deposit letter) and the physical count in bundles delivered to the CBM Branch/CTC.', N'At the Point of Transaction', N'per mismatched denomination', NULL, 300000, NULL, NULL, NULL, 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Penalties] WHERE [Name] = N'Discrepancies in labelling of bundles')
INSERT INTO [Penalties] ([Name], [Description], [AppliesTo], [Unit], [CutOffTime], [PenaltyValue], [PenaltyPercent], [PercentCutOff], [PenaltyMultiplier], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES (N'Discrepancies in labelling of bundles', N'Discrepancies in the labelling of bundles in deposits.', N'At the Point of Transaction', NULL, NULL, 200000, NULL, NULL, NULL, 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Penalties] WHERE [Name] = N'Inappropriate packaging (torn, strings)')
INSERT INTO [Penalties] ([Name], [Description], [AppliesTo], [Unit], [CutOffTime], [PenaltyValue], [PenaltyPercent], [PercentCutOff], [PenaltyMultiplier], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES (N'Inappropriate packaging (torn, strings)', N'Inappropriate packaging (torn, strings) when depositing.', N'At the Point of Transaction', N'per torn package', NULL, 10000, NULL, NULL, NULL, 1, GETDATE(), GETDATE(), N'system', N'system');");

            // ── Processing ─────────────────────
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Penalties] WHERE [Name] = N'Fit notes in unfit notes and vice versa')
INSERT INTO [Penalties] ([Name], [Description], [AppliesTo], [Unit], [CutOffTime], [PenaltyValue], [PenaltyPercent], [PercentCutOff], [PenaltyMultiplier], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES (N'Fit notes in unfit notes and vice versa', N'Detection of fit notes in unfit notes and vice versa. A range above 70% of the entire consignment attracts no charge; anything below 70% attracts a charge.', N'Processing', NULL, NULL, NULL, 0.001, 0.70, NULL, 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Penalties] WHERE [Name] = N'Mix-up of denominations within bundles')
INSERT INTO [Penalties] ([Name], [Description], [AppliesTo], [Unit], [CutOffTime], [PenaltyValue], [PenaltyPercent], [PercentCutOff], [PenaltyMultiplier], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES (N'Mix-up of denominations within bundles', N'Mix-up of denominations within bundles deposited.', N'Processing', N'per piece short', NULL, NULL, NULL, NULL, 5, 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Penalties] WHERE [Name] = N'Discrepancies in deposits — missing pieces')
INSERT INTO [Penalties] ([Name], [Description], [AppliesTo], [Unit], [CutOffTime], [PenaltyValue], [PenaltyPercent], [PercentCutOff], [PenaltyMultiplier], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES (N'Discrepancies in deposits — missing pieces', N'Discrepancies in deposits of coins/notes of all denominations; the shortage must also be made good.', N'Processing', N'per missing piece', NULL, NULL, NULL, NULL, 5, 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Penalties] WHERE [Name] = N'Counterfeit notes/coins found')
INSERT INTO [Penalties] ([Name], [Description], [AppliesTo], [Unit], [CutOffTime], [PenaltyValue], [PenaltyPercent], [PercentCutOff], [PenaltyMultiplier], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES (N'Counterfeit notes/coins found', N'Counterfeit notes and/or coins found in a consignment.', N'Processing', N'per piece', NULL, NULL, NULL, NULL, 10, 1, GETDATE(), GETDATE(), N'system', N'system');");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [Penalties] WHERE [Name] = N'Defaced notes found')
INSERT INTO [Penalties] ([Name], [Description], [AppliesTo], [Unit], [CutOffTime], [PenaltyValue], [PenaltyPercent], [PercentCutOff], [PenaltyMultiplier], [Active], [CreatedDate], [ModifiedDate], [CreatedBy], [ModifiedBy])
VALUES (N'Defaced notes found', N'Defaced notes found in a consignment.', N'Processing', N'per piece', NULL, NULL, NULL, NULL, 5, 1, GETDATE(), GETDATE(), N'system', N'system');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM [Penalties] WHERE [Name] IN (
    N'Late submission of letter of intent to transact',
    N'Late submission of RTGS to transact',
    N'Transacting late (after 3:00pm)',
    N'Failure to cancel in time',
    N'Failure to amend in time',
    N'Use of unauthorised personnel/agents and CIT',
    N'Error on the letter of intent and RTGS',
    N'Mismatch of value/category vs physical count',
    N'Discrepancies in labelling of bundles',
    N'Inappropriate packaging (torn, strings)',
    N'Fit notes in unfit notes and vice versa',
    N'Mix-up of denominations within bundles',
    N'Discrepancies in deposits — missing pieces',
    N'Counterfeit notes/coins found',
    N'Defaced notes found'
);");
        }
    }
}
