using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Core.Repositories.Stocks
{
    public interface IStockLedgerDetailRepository
    {
        Task<bool> Create(StockLedgerDetail model);
        Task<ICollection<StockLedgerDetail>> FindAll();
        Task<StockLedgerDetail> FindById(int id);
        Task<bool> IsExists(int id);
        Task<bool> Update(StockLedgerDetail model);
    }
}