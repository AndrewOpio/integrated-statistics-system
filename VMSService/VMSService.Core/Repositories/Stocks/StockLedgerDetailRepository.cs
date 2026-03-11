using Microsoft.Extensions.Logging;
using VMSService.Core.Models.StockLedgers;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.Stocks
{
    public class StockLedgerDetailRepository(IStockService _stockService, ILogger<StockLedgerDetailRepository> _logger) : IStockLedgerDetailRepository
    {
        public async Task<bool> Create(StockLedgerDetail model)
        {
            try
            {
                return await _stockService.CreateStockLedgerDetail(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stock ledger detail");
                return false;
            }
        }

        public async Task<ICollection<StockLedgerDetail>> FindAll()
        {
            try
            {
                var stockLedgerDetails = await _stockService.GetAllStockLedgerDetails();
                return stockLedgerDetails.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all stock ledger details");
                return new List<StockLedgerDetail>();
            }
        }

        public async Task<StockLedgerDetail> FindById(int id)
        {
            try
            {
                var stockLedgerDetail = await _stockService.GetStockLedgerDetailById(id);
                if (stockLedgerDetail == null)
                    return null;

                return stockLedgerDetail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock ledger detail with id {StockLedgerDetailId}", id);
                return null;
            }
        }

        public async Task<bool> IsExists(int id)
        {
            try
            {
                var stockLedgerDetail = await FindById(id);
                return stockLedgerDetail != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if stock ledger detail exists with id {StockLedgerDetailId}", id);
                return false;
            }
        }

        public async Task<bool> Update(StockLedgerDetail model)
        {
            try
            {
                return await _stockService.UpdateStockLedgerDetail(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock ledger detail");
                return false;
            }
        }
    }
}