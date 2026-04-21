namespace VMSService.Core.Models.CommercialBankUser
{
    public class CommercialBankUserViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int CommercialBankId { get; set; }
        public int? CommercialBankBranchId { get; set; }
    }
}
