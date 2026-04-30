namespace VMSService.Core.Models.CbDepositRequest
{
    public class DepositDenominationLineViewModel
    {
        public string DenominationLabel { get; init; } = string.Empty;
        public int? Bundles { get; init; }
        public int? Boxes { get; init; }
        public decimal Amount { get; init; }
        /// <summary>
        /// 1 = Fit, 2 = Unfit, 3 = N/A
        /// </summary>
        public int SubCategoryId { get; init; }
        public string SubCategoryName { get; init; } = string.Empty;
    }
}