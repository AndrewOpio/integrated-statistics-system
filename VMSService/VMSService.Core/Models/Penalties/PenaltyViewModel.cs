using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VMSService.Core.Models.Penalties
{
    public class PenaltyViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string AppliesTo { get; set; } = string.Empty;

        public string? Unit { get; set; }

        public TimeOnly? CutOffTime { get; set; }

        public decimal? PenaltyValue { get; set; }

        public decimal? PenaltyPercent { get; set; }

        public decimal? PercentCutOff { get; set; }

        public decimal? PenaltyMultiplier { get; set; }

        public bool Active { get; set; } = true;

        public List<SelectListItem> AppliesToList { get; set; } = new();
    }
}
