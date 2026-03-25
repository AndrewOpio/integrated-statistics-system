using StockService.Core.Models.CounterPartyTypes;
using StockService.Core.Models.BranchCategories;
using StockService.Core.Models.Categories;

namespace StockService.Core.Models.StockLedger
{
    public class StockLedgerDto
    {
        public int Id { get; set; }
        public string ReferenceCode { get; set; }
        public bool IsIn { get; set; }
        public int CounterPartyTypeId { get; set; }
        public CounterPartyTypeDto? CounterPartyType { get; set; }
        public int CounterPartyId { get; set; }
        public int StoreId { get; set; }
        public string Notes { get; set; }
        public List<StockLedgerDetailDto> StockLedgerDetails { get; set; }
    }
}
