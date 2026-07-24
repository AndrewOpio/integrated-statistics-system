using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CfbTopupStatusHistories
{
    public interface ITopupRequestStatusHistoryRepository
    {
        Task Create(TopupRequestStatusHistory history);
        Task<bool> Save();
    }
}
