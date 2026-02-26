namespace StockService.Core.Repository.Common
{
    public interface IRepositoryBase<T, TId> where T : class
    {
        Task<ICollection<T>> FindAllAsync();
        Task<T> FindByIdAsync(TId id);
        Task<T> CreateAsync(T entity);
        Task<bool> UpdateAsync(T entity);
        //Task<bool> IsExists(TId id);
        Task<bool> Save();
    }
}
