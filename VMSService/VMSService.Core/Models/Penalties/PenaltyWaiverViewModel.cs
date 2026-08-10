using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VMSService.Core.Models.Penalties
{
    public class PenaltyWaiverViewModel
    {
        public int Id { get; set; }

        [Required]
        public int PenaltyId { get; set; }
        public string PenaltyName { get; set; } = string.Empty;

        [Required]
        public int BouBranchId { get; set; }
        public string BouBranchCode { get; set; } = string.Empty;
        public string BouBranchName { get; set; } = string.Empty;

        [Required]
        public DateOnly StartDate { get; set; }

        [Required]
        public DateOnly EndDate { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        public bool Active { get; set; } = true;

        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }

        public List<SelectListItem> Penalties { get; set; } = new();
        public List<SelectListItem> BouBranches { get; set; } = new();
    }
}
