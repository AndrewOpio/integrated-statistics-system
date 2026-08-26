using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.WithdrawalRequests
{
    public interface IWithdrawalRequestRepository : IRepositoryBase<WithdrawalRequest, Guid>
    {
        Task<WithdrawalRequest?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
        Task<IEnumerable<WithdrawalRequest>> GetByCbAsync(int cbId, CancellationToken ct = default);
        Task<int> CountByCbBranchAndDateAsync(int cbId, int bouBranchId, DateOnly date, CancellationToken ct = default);
        /// <summary>Requests stuck at Pending RTGS Code without a code, whose collection date
        /// has arrived (on or before <paramref name="asOfDate"/>) — used by the daily expiry job.</summary>
        Task<IEnumerable<WithdrawalRequest>> GetPendingRtgsWithoutCodeAsync(DateOnly asOfDate, CancellationToken ct = default);

        /// <summary>CB landing-page main list: this CB's non-deleted requests, excluding any that
        /// are both terminal (<see cref="Static.WithdrawConstants.WithdrawalStatusNames.TerminalStatuses"/>)
        /// and whose collection date has already passed — those live in <see cref="GetHistoryByCbAsync"/>
        /// instead. A terminal request whose collection date is today still shows here.</summary>
        Task<IEnumerable<WithdrawalRequest>> GetActiveByCbAsync(int cbId, DateOnly today, CancellationToken ct = default);

        /// <summary>CB History view: this CB's non-deleted requests that are both terminal and past
        /// their collection date, bounded to <paramref name="since"/> (12 months back from today) —
        /// older than that, the CB asks BOU directly.</summary>
        Task<IEnumerable<WithdrawalRequest>> GetHistoryByCbAsync(int cbId, DateOnly today, DateOnly since, CancellationToken ct = default);

        /// <summary>BOU landing-page main list — same terminal-and-past exclusion as the CB side,
        /// optionally scoped to one BOU branch (null/empty means every branch).</summary>
        Task<IEnumerable<WithdrawalRequest>> GetActiveAsync(DateOnly today, string? bouBranchCode = null, CancellationToken ct = default);

        /// <summary>BOU History view — mirrors <see cref="GetHistoryByCbAsync"/>, optionally branch-scoped.</summary>
        Task<IEnumerable<WithdrawalRequest>> GetHistoryAsync(DateOnly today, DateOnly since, string? bouBranchCode = null, CancellationToken ct = default);

        /// <summary>Lifetime sum of Approved requests' TotalAmount — deliberately unaffected by the
        /// active/history split, since "Total Processed" is a running total, not a recent-activity
        /// count. Optionally scoped to one BOU branch.</summary>
        Task<decimal> SumApprovedAmountAsync(string? bouBranchCode = null, CancellationToken ct = default);

        /// <summary>Fast pre-check for RTGS code uniqueness — friendly-UX companion to the database's
        /// filtered unique index (the real guarantee). Only live (non-deleted) requests count, so a
        /// soft-deleted request's code is reported as available for reuse. <paramref name="excludeId"/>
        /// lets an edit re-save its own unchanged code without tripping over itself.</summary>
        Task<bool> ExistsByRtgsCodeAsync(string rtgsCode, Guid? excludeId = null, CancellationToken ct = default);
    }
}
