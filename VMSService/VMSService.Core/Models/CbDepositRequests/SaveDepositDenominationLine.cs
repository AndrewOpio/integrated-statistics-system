namespace VMSService.Core.Models.CbDepositRequest
{
    public class SaveDepositDenominationLine
    {
        public int DenominationId { get; set; }
        public int DenominationValue { get; set; }
        public int DenominationTypeId { get; set; }
        public int? Bundles { get; set; }             // for notes
        public int? Boxes { get; set; }               // for coins
        public decimal Amount { get; set; }
        /// <summary>
        /// 1 = Fit, 2 = Unfit, 3 = N/A
        /// </summary>
        public int SubCategoryId { get; set; }
    }
}