using VMSService.Core.Models.Denominations;
using VMSService.Core.Models.DenominationTypes;
using VMSService.Core.Models.StockLedgers;
using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Data.Entities;

namespace VMSService.Core.Services.Stocks
{
    public interface IStockService
    {
        // DenominationType methods
        Task<ICollection<DenominationType>> GetAllDenominationTypes();
        Task<DenominationType> GetDenominationTypeById(int id);
        Task<bool> CreateDenominationType(DenominationType denominationType);
        Task<bool> UpdateDenominationType(DenominationType denominationType);

        // Denomination methods
        Task<ICollection<Denomination>> GetAllDenominations();
        Task<Denomination> GetDenominationById(int id);
        Task<bool> CreateDenomination(Denomination denomination);
        Task<bool> UpdateDenomination(Denomination denomination);

        // StockLedger methods
        Task<ICollection<StockLedger>> GetAllStockLedgers();
        Task<StockLedger> GetStockLedgerById(int id);
        Task<StockLedger> CreateStockLedger(StockLedger stockLedger);

        // StockLedgerDetail methods
        Task<ICollection<StockLedgerDetail>> GetAllStockLedgerDetails();
        Task<StockLedgerDetail> GetStockLedgerDetailById(int id);
        Task<bool> CreateStockLedgerDetail(StockLedgerDetail stockLedgerDetail);
        Task<bool> UpdateStockLedgerDetail(StockLedgerDetail stockLedgerDetail);
    }
}