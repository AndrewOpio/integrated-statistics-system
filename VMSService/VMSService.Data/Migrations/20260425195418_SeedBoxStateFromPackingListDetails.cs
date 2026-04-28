using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VMSService.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedBoxStateFromPackingListDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO BoxStates (
                    PackingListDetailsId,
                    CaseNumber,
                    DenominationId,
                    BranchIsAtId,
                    CurrentSerialCursor,
                    RemainingBundles,
                    IsAvailable,
                    CreatedBy,
                    CreatedDate,
                    ModifiedBy,
                    ModifiedDate,
                    IsDeleted
                )
                SELECT
                    pd.Id,
                    CAST(pd.CaseNumber AS NVARCHAR(50)),
                    emd.DenominationId,
                    COALESCE(latest_internal.ReceivingBranchId, 8),
                    CAST(pd.SerialNoteStartNumber AS BIGINT),
                    50,
                    1,
                    'system-seed',
                    SYSUTCDATETIME(),
                    'system-seed',
                    SYSUTCDATETIME(),
                    0
                FROM PackingListDetails pd
                INNER JOIN PackingListRanges pr ON pd.PackingListRangeId = pr.Id
                INNER JOIN ExPrintMintDetails emd ON pr.ExPrintMintDetailId = emd.Id
                INNER JOIN ExPrintMints em ON emd.ExPrintMintId = em.Id
                OUTER APPLY (
                    SELECT TOP 1 sm.ReceivingBranchId
                    FROM StockMovementDenominationBoxNumberRanges r
                    INNER JOIN StockMovementDenominations smd ON r.StockMovementDenominationId = smd.Id
                    INNER JOIN StockMovements sm ON smd.StockMovementId = sm.Id
                    WHERE sm.MovementType = 'BranchToBranch'
                      AND smd.DenominationId = emd.DenominationId
                      AND TRY_CAST(r.[Start] AS INT) <= pd.CaseNumber
                      AND TRY_CAST(r.[End] AS INT) >= pd.CaseNumber
                    ORDER BY sm.[Date] DESC, sm.Id DESC
                ) AS latest_internal
                WHERE NOT EXISTS (
                    SELECT 1 FROM BoxStates bs WHERE bs.PackingListDetailsId = pd.Id
                )
                  AND emd.IsDeleted = 0
                  AND em.IsDeleted = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {// Remove only rows we seeded. If any state rows were created post-seed by application logic,
            // they have a different CreatedBy and won't be touched.
            migrationBuilder.Sql(@"
                DELETE FROM BoxStates WHERE CreatedBy = 'system-seed';
            ");

        }
    }
}
