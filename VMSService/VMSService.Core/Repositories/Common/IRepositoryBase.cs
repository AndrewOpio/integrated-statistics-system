namespace VMSService.Core.Repository.Common
{
    public interface IRepositoryBase<T, TId> where T : class
    {
        Task<ICollection<T>> FindAll();
        Task<T> FindById(TId id);
        Task<bool> Create(T entity);
        Task<bool> Update(T entity);
        Task<bool> IsExists(TId id);
        Task<bool> Save();
    }
}
