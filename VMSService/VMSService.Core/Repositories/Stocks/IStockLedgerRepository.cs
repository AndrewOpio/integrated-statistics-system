using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Core.Repositories.Stocks
{
    public interface IStockLedgerRepository
    {
        Task<StockLedger> Create(StockLedger model);
        Task<ICollection<StockLedger>> FindAll();
        Task<StockLedger> FindById(int id);
        Task<bool> IsExists(int id);
    }
}