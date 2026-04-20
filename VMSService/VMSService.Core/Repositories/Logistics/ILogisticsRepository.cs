using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.Logistics
{
    public interface ILogisticsRepository
    {
        Task<List<Data.Entities.Logistics>> GetAllAsync();
        Task<Data.Entities.Logistics?> GetByIdAsync(int id);
        Task<Data.Entities.Logistics> CreateAsync(Data.Entities.Logistics logistics);
        Task UpdateAsync(Data.Entities.Logistics logistics);
        Task DeleteAsync(int id);
    }
}
