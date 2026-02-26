using StockService.Core.Repository.Common;
using StockService.Data.Entities;

namespace StockService.Core.Repository.StockLedgers
{
    public interface IStockLedgerRepository : IRepositoryBase<StockLedger, int>
    {
    }
}