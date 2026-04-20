using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.PackingListRangeDetails
{
    public interface IPackingListDetailsRepository
    {
        Task<ICollection<PackingListDetails>> FindByPackingListRangeIdAsync(int packingListRangeId);
        Task<PackingListDetails> FindByIdAsync(long id);
        Task<PackingListDetails> CreateAsync(PackingListDetails entity);
        Task<bool> CreateBatchAsync(List<PackingListDetails> entities);
        Task<bool> DeleteByRangeIdAsync(int rangeId);
        Task<bool> SaveAsync();
    }
}
