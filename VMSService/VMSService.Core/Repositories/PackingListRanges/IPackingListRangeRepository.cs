using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.PackingListRanges
{
    public interface IPackingListRangeRepository
    {
        Task<ICollection<PackingListRange>> FindAllAsync();
        Task<PackingListRange> FindByIdAsync(int id);
        Task<ICollection<PackingListRange>> FindByExStockIdAsync(int exStockId);
        Task<ICollection<PackingListRange>> FindAllActiveAsync();
        Task<PackingListRange> CreateAsync(PackingListRange entity);
        Task<bool> UpdateAsync(PackingListRange entity);
        Task<bool> DeleteAsync(int id);
        Task<bool> SaveAsync();
    }
}
