using Microsoft.Extensions.Logging;
using VMSService.Core.Models.StockLedgers;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;

namespace VMSService.Core.Repositories.Stocks
{
    public class StockLedgerRepository(IStockService _stockService, ILogger<StockLedgerRepository> _logger) : IStockLedgerRepository
    {
        public async Task<StockLedger> Create(StockLedger model)
        {
            try
            {
                return await _stockService.CreateStockLedger(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stock ledger");
                return null;
            }
        }

        public async Task<ICollection<StockLedger>> FindAll()
        {
            try
            {
                var stockLedgerRecords = await _stockService.GetAllStockLedgers();
                return stockLedgerRecords.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all stock ledgers");
                return new List<StockLedger>();
            }
        }

        public async Task<StockLedger> FindById(int id)
        {
            try
            {
                var stockLedgerRecord = await _stockService.GetStockLedgerById(id);
                if (stockLedgerRecord == null)
                    return null;

                return stockLedgerRecord;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock ledger with id {StockLedgerId}", id);
                return null;
            }
        }

        public async Task<bool> IsExists(int id)
        {
            try
            {
                var stockLedger = await FindById(id);
                return stockLedger != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if stock ledger exists with id {StockLedgerId}", id);
                return false;
            }
        }
    }
}