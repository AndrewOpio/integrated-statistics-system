using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CfbTopupStatusHistories
{
    public class TopupRequestStatusHistoryRepository(VMSServiceDbContext _db) : ITopupRequestStatusHistoryRepository
    {
        public async Task Create(TopupRequestStatusHistory history)
            => await _db.TopupRequestStatusHistories.AddAsync(history);

        public async Task<bool> Save()
            => await _db.SaveChangesAsync() > 0;
    }
}
