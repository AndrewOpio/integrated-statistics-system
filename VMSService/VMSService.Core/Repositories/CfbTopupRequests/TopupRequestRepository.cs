using Microsoft.EntityFrameworkCore;
using VMSService.Core.Static;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CfbTopupRequests
{
    public class TopupRequestRepository(VMSServiceDbContext _db) : ITopupRequestRepository
    {
        private static readonly string[] TerminalStatuses =
        [
            CfbConstants.CfbStatusNames.Approved,
            CfbConstants.CfbStatusNames.Rejected,
        ];

        public async Task<TopupRequest?> FindById(int id)
            => await _db.TopupRequests
                .Include(r => r.Lines)
                .Include(r => r.StatusHistory.Where(h => !h.IsDeleted).OrderBy(h => h.ChangedAt))
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<TopupRequest?> FindByIdWithLines(int id)
            => await _db.TopupRequests
                .Include(r => r.Lines)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<ICollection<TopupRequest>> FindByBranchId(int bouBranchId)
            => await _db.TopupRequests
                .Where(r => r.BouBranchId == bouBranchId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

        public async Task<ICollection<TopupRequest>> FindAll()
            => await _db.TopupRequests
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

        public async Task<bool> HasPendingForBranch(int bouBranchId)
            => await _db.TopupRequests
                .AnyAsync(r => r.BouBranchId == bouBranchId
                            && !TerminalStatuses.Contains(r.CurrentStatusName));

        public async Task<bool> IsExists(int id)
            => await _db.TopupRequests.AnyAsync(r => r.Id == id);

        public async Task<bool> Create(TopupRequest request)
        {
            await _db.TopupRequests.AddAsync(request);
            return true;
        }

        public async Task<bool> Update(TopupRequest request)
        {
            _db.TopupRequests.Update(request);
            return await Save();
        }

        public async Task<bool> Save()
            => await _db.SaveChangesAsync() > 0;
    }
}
