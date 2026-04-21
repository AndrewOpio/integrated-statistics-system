using System.Collections.Generic;
using System.Threading.Tasks;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.Logistics
{
    public interface ILogisticsStatusHistoryRepository
    {
        Task<List<LogisticsStatusHistory>> GetByLogisticsIdAsync(int logisticsId);
        Task CreateAsync(LogisticsStatusHistory statusHistory);
    }
}
