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

namespace VMSService.Core.Repositories.WithdrawalRequests
{

    public class WithdrawalRequestRepository(
        VMSServiceDbContext _db) : IWithdrawalRequestRepository
    {
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


        public async Task<bool> IsExists(Guid id)
        => await _db.WithdrawalRequests.AnyAsync(d => d.Id == id);

        public Task<bool> Save()
            => Task.FromResult(true);

        //transactionReferenceNumber

    }
}
