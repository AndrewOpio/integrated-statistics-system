using VMSService.Core.Repository.Common;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Core.Repositories.CounterPartyTypes
{
    public interface ICounterPartyTypeRepository : IRepositoryBase<CounterPartyType, int>
    {
        Task<CounterPartyType> FindByName(string name);
    }
}