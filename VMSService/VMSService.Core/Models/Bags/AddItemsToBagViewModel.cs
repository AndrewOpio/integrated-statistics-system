namespace VMSService.Core.Models.Bags
{
    public class AddItemsToBagViewModel
    {
        public int BagId { get; set; }
        public List<Guid> DepositRequestDenominationIds { get; set; } = [];
    }
}