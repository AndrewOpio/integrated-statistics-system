using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Data.Entities;

namespace VMSService.Core.Models.CbDepositRequests
{
    public class DepositDenomTableViewModel
    {
        public IEnumerable<Denomination> Notes { get; init; } = [];
        public IEnumerable<Denomination> Coins { get; init; } = [];
        public List<DepositRequestDenomination> ExistingLines { get; set; } = [];
        public IEnumerable<SubCategory> SubCategories { get; init; } = [];
        public List<ExistingDenominationViewModel> ExistingDenominations { get; set; } = new();
    }
}