using StockService.Data.Entities;

namespace StockService.Dto
{
    public class DenominationDto
    {
        public decimal Value { get; init; }
        public string DisplayLabel { get; init; } = string.Empty;
        public DenominationType DenominationType { get; init; }
        public int SortOrder { get; init; }

    }

    public enum DenominationType
    {
        Note,
        Coin
    }
}
