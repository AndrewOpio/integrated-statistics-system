using VMSService.Core.Services.Stocks.StockEntities;
using VMSService.Data.Entities;

namespace VMSService.Core.Models.StockLedgers
{
    public class StockLedgerViewModel
    {
        public int Id { get; set; }
        public string ReferenceCode { get; set; }
        public bool IsIn { get; set; }
        public int CounterPartyTypeId { get; set; }
        public CounterPartyTypeViewModel CounterPartyType { get; set; }
        public int CounterPartyId { get; set; }
        public int CategoryId { get; set; }
        public CategoryViewModel Category { get; set; }
        public string Notes { get; set; }
        public ICollection<StockLedgerDetailViewModel> StockLedgerDetails { get; set; }
    }

    public class StockLedgerDetailViewModel
    {
        public int Id { get; set; }
        public int DenominationId { get; set; }
        public Denomination? Denomination { get; set; }
        public int Count { get; set; }
        public int Values { get; set; }
        public int StockLedgerId { get; set; }
        public StockLedger? StockLedger { get; set; }
    }

    public class CounterPartyTypeViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}