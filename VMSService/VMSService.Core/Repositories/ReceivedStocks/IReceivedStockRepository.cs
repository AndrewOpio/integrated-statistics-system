using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.ReceivedStocks
{
    public interface IReceivedStockRepository : IRepositoryBase<ReceivedStock, int>
    {
        Task<int> CreateNew(ReceivedStock model);
    }
}