using StockService.Core.Models.Categories;
using StockService.Core.Models.DenominationTypes;

namespace StockService.Core.Models.Denominations
{
    public class DenominationDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? DenominationTypeId { get; set; }
        public decimal Value { get; set; }
        public int NumberOfPiecesInABox { get; set; }
        public DenominationTypeDto? DenominationType { get; set; }
        public bool IsDeleted { get; set; }
    }
}
