namespace VMSService.Core.Models.StockProcessingPlans
{
    public class CreateLegacyBagRequest
    {
        public string CommercialBankName { get; set; } = string.Empty;
        public DateOnly DepositDate { get; set; }
        public int SourceDepositBranchId { get; set; }
        public int DenominationId { get; set; }
        public decimal Value { get; set; }
        public int BranchId { get; set; }
    }
}
