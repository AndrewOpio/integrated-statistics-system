using StockService.Core.Models.Categories;
using StockService.Core.Models.Denominations;
using StockService.Core.Models.SubCategories;
using StockService.Data.Entities;

namespace StockService.Core.Models.StockLedger
{
    public class StockLedgerDetailDto
    {
        public int Id { get; set; }
        public int DenominationId { get; set; }
        public DenominationDto? Denomination { get; set; }
        public int CategoryId { get; set; }
        public CategoryDto? Category { get; set; }
        public int NumberOfPieces { get; set; }
        public int NumberOfBoxes { get; set; }
        public int? NumberOfBundles { get; set; }
        public decimal Value { get; set; }
        public int? SubCategoryId { get; set; }
        public SubCategoryDto? SubCategory { get; set; }
    }
}
