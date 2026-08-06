using System.Globalization;

namespace VMSService.Core.Services.Stocks.StockEntities
{
    public class StockLedgerDetailReportItem
    {
        public int StockLedgerId { get; set; }
        public string ReferenceCode { get; set; }
        public int StockMovementId { get; set; }
        public int BranchId { get; set; }
        public bool IsIn { get; set; }
        public int CounterPartyTypeId { get; set; }
        public int CounterPartyId { get; set; }
        public string Notes { get; set; }
        public DateTime StockLedgerCreatedDate { get; set; }

        public int StockLedgerDetailId { get; set; }
        public int DenominationId { get; set; }
        public string? Denomination { get; set; }
        public decimal? DenominationValue { get; set; }
        public int CategoryId { get; set; }
        public string? Category { get; set; }
        public int? SubCategoryId { get; set; }
        public string? SubCategory { get; set; }
        public int NumberOfPieces { get; set; }
        public int? NumberOfBundles { get; set; }
        public int NumberOfBoxes { get; set; }

        // The API decrypts this before returning it, so it holds a plain decimal string
        public string EncryptedValue { get; set; }

        public decimal Value =>
            decimal.TryParse(EncryptedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0;
    }
}
