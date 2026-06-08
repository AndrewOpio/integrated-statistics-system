using Microsoft.AspNetCore.Mvc.Rendering;

namespace VMSService.Core.Models.MinDepositStandards
{
    public class MinDepositStandardViewModel
    {
        public int Id { get; set; }
        public int DenominationId { get; set; }
        public int BranchId { get; set; }
        public int MinimumBundles { get; set; }
        public decimal Value { get; set; }

        // Display names
        public string? DenominationName { get; set; }
        public string? BranchName { get; set; }

        // Dropdown lists
        public List<SelectListItem> DenominationList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> BranchList { get; set; } = new List<SelectListItem>();
    }
}