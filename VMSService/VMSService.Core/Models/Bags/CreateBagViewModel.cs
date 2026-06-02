namespace VMSService.Core.Models.Bags
{
    public class CreateBagViewModel
    {
        public string ReferenceNumber { get; set; } = string.Empty;
        public int DenominationId { get; set; }
    }
}