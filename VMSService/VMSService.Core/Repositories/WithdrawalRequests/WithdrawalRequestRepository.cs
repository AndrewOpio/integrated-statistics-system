using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using VMSService.Core.Repositories.WithdrawalTransactionRefNumberGenerators;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;
using WorkflowService.Client;
using static VMSService.Core.Static.WithdrawConstants;

namespace VMSService.Core.Repositories.WithdrawalRequests
{

    public class WithdrawalRequestRepository(
        VMSServiceDbContext _db) : IWithdrawalRequestRepository
    {
        /// <summary>Section 10's stale-pre-submission flag — a request still stuck before CB
        /// Approver submission (Draft/Pending Internal Approval/Returned for Internal Revision)
        /// whose collection date has already passed. Shared by the row list and Details page so
        /// both agree on the same rule.</summary>
        public static bool IsStalePreSubmission(string statusName, DateOnly collectionDate, DateOnly today)
            => WithdrawalStatusNames.PreSubmissionStatuses.Contains(statusName) && collectionDate < today;

        public async Task<ICollection<WithdrawalRequest>> FindAll()
            => await _db.WithdrawalRequests.ToListAsync();

        public async Task<WithdrawalRequest> FindById(Guid id)
            => await _db.WithdrawalRequests.FindAsync(id)
            ?? throw new KeyNotFoundException($"WithdrawalRequest {id} not found.");


        public async Task<bool> Create(WithdrawalRequest entity)
        {
            await _db.WithdrawalRequests.AddAsync(entity);
            return true;
        }

        public Task<bool> Update(WithdrawalRequest entity)
        {
            _db.WithdrawalRequests.Update(entity);
            return Task.FromResult(true);
        }

        public async Task<WithdrawalRequest?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
            => await _db.WithdrawalRequests
            .Include(r => r.WithdrawalRequestDenominations)
            .ThenInclude(d => d.DispatchedBoxes)
            .Include(r => r.WithdrawalRequestAgents)
            .Include(r => r.WithdrawalRequestStatusHistory)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        public async Task<IEnumerable<WithdrawalRequest>> FindAllBouRequestAsync(CancellationToken ct = default)
        {
            //var statusType = _workflowServiceClient.FindStatusById(request.StatusTypeId);

            return await _db.WithdrawalRequests
            .Include(r => r.WithdrawalRequestDenominations)
            .Include(r => r.WithdrawalRequestAgents)
            //.Include(r => r.WithdrawalRequestStatusHistory)
            .ToListAsync(ct);
        }


        public async Task<int> CountByCbBranchAndDateAsync(int cbId, int bouBranchId, DateOnly date, CancellationToken ct = default)
        => await _db.WithdrawalRequests
            .CountAsync(r => r.CbId == cbId
                          && r.BouBranchId == bouBranchId
                          && r.CollectionDate == date, ct);

        public async Task<IEnumerable<WithdrawalRequest>> GetByCbAsync(int cbId, CancellationToken ct = default)
        => await _db.WithdrawalRequests
            .Where(r => r.CbId == cbId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync(ct);


        public async Task<IEnumerable<WithdrawalRequest>> GetPendingRtgsWithoutCodeAsync(DateOnly asOfDate, CancellationToken ct = default)
            => await _db.WithdrawalRequests
                .Where(r => !r.IsDeleted
                    && r.StatusTypeName == WithdrawalStatusNames.PendingRTGSVerification
                    && r.CollectionDate <= asOfDate
                    && (r.RtgsCode == null || r.RtgsCode == string.Empty))
                .ToListAsync(ct);

        public async Task<IEnumerable<WithdrawalRequest>> GetActiveByCbAsync(int cbId, DateOnly today, CancellationToken ct = default)
        => await _db.WithdrawalRequests
            .Where(r => r.CbId == cbId
                     && !r.IsDeleted
                     && !(WithdrawalStatusNames.TerminalStatuses.Contains(r.StatusTypeName) && r.CollectionDate < today))
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync(ct);

        public async Task<IEnumerable<WithdrawalRequest>> GetHistoryByCbAsync(int cbId, DateOnly today, DateOnly from, DateOnly to, CancellationToken ct = default)
        => await _db.WithdrawalRequests
            .Where(r => r.CbId == cbId
                     && !r.IsDeleted
                     && WithdrawalStatusNames.TerminalStatuses.Contains(r.StatusTypeName)
                     && r.CollectionDate < today
                     && r.CollectionDate >= from
                     && r.CollectionDate <= to)
            .OrderByDescending(r => r.CollectionDate)
            .ToListAsync(ct);

        public async Task<IEnumerable<WithdrawalRequest>> GetActiveAsync(DateOnly today, string? bouBranchCode = null, CancellationToken ct = default)
        => await _db.WithdrawalRequests
            .Where(r => !r.IsDeleted
                     && !(WithdrawalStatusNames.TerminalStatuses.Contains(r.StatusTypeName) && r.CollectionDate < today)
                     && (string.IsNullOrEmpty(bouBranchCode) || r.BouBranchCode == bouBranchCode))
            .ToListAsync(ct);

        public async Task<IEnumerable<WithdrawalRequest>> GetHistoryAsync(DateOnly today, DateOnly from, DateOnly to, string? bouBranchCode = null, CancellationToken ct = default)
        => await _db.WithdrawalRequests
            .Where(r => !r.IsDeleted
                     && WithdrawalStatusNames.TerminalStatuses.Contains(r.StatusTypeName)
                     && r.CollectionDate < today
                     && r.CollectionDate >= from
                     && r.CollectionDate <= to
                     && (string.IsNullOrEmpty(bouBranchCode) || r.BouBranchCode == bouBranchCode))
            .OrderByDescending(r => r.CollectionDate)
            .ToListAsync(ct);

        public async Task<decimal> SumApprovedAmountAsync(string? bouBranchCode = null, CancellationToken ct = default)
        => await _db.WithdrawalRequests
            .Where(r => !r.IsDeleted
                     && r.StatusTypeName == WithdrawalStatusNames.Approved
                     && (string.IsNullOrEmpty(bouBranchCode) || r.BouBranchCode == bouBranchCode))
            .SumAsync(r => r.TotalAmount, ct);

        public async Task<bool> IsExists(Guid id)
        => await _db.WithdrawalRequests.AnyAsync(d => d.Id == id);

        public async Task<bool> ExistsByRtgsCodeAsync(string rtgsCode, Guid? excludeId = null, CancellationToken ct = default)
        => await _db.WithdrawalRequests
            .AnyAsync(r => !r.IsDeleted
                        && r.RtgsCode == rtgsCode
                        && (!excludeId.HasValue || r.Id != excludeId.Value), ct);

        public Task<bool> Save()
            => Task.FromResult(true);

        //transactionReferenceNumber

    }
}
