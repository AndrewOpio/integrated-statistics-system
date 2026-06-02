using VMSService.Core.Models.CbDepositRequests;

namespace VMSService.Core.Models.Bags
{
    public class BagDetailsViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public int DenominationId { get; set; }
        public string? DenominationName { get; set; }
        public decimal DenominationValue { get; set; }
        public List<DepositRequestDenominationViewModel> Items { get; set; } = [];
        public decimal TotalAmount { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}