namespace VMSService.Core.Models.CbDepositRequests
{
    public class DepositRequestDenominationViewModel
    {
        public Guid Id { get; set; }
        public Guid DepositRequestId { get; set; }
        public string? TransactionRefNumber { get; set; }
        public int DenominationId { get; set; }
        public string? DenominationName { get; set; }
        public decimal DenominationValue { get; set; }
        public int DenominationTypeId { get; set; }
        public string? DenominationTypeName { get; set; }
        public int? Bundles { get; set; }
        public int? Boxes { get; set; }
        public decimal Amount { get; set; }
        public string Origin { get; set; } = "Cb";
        public int SubCategoryId { get; set; }
        public string? SubCategoryName { get; set; }
        public bool IsInBag { get; set; }
        public DateOnly DepositDate { get; set; }
        public int CbId { get; set; }
        public string? CommercialBankName { get; set; }
        public int NumberOfBundlesOrBoxes { get; set; }
    }
}