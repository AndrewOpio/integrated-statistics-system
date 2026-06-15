using Microsoft.AspNetCore.Mvc.Rendering;
using VMSService.Core.Models.DepositRequests;

namespace VMSService.Core.Models.DepositFees
{
    public class DepositFeeViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int FeeTypeId { get; set; }
        public string? FeeTypeName { get; set; }
        public Guid DepositRequestId { get; set; }
        public BouDepositRequestViewModel? DepositRequest { get; set; }
        public string? DepositRequestRefNumber { get; set; }
        public decimal Amount { get; set; }
        public bool IsWaived { get; set; } = false;
        public string? WaiverReason { get; set; }
        public DateTime? WaivedDate { get; set; }
        public string? WaivedBy { get; set; }

        // Dropdown lists
        public List<SelectListItem> FeeTypeList { get; set; } = new List<SelectListItem>();
    }
}