using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Data.Entities;

namespace VMSService.Core.Models.CbDepositRequest
{
    public class DepositDenomTableViewModel
    {
        public IEnumerable<Denomination> Notes { get; init; } = [];
        public IEnumerable<Denomination> Coins { get; init; } = [];
        public List<DepositRequestDenomination> ExistingLines { get; set; } = [];
    }
}