using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace VMSService.Core.Models.BagMovements
{
    public class CreateBagMovementViewModel
    {
        // Null on Create, set on Edit — lets one view model serve both pages.
        public int? Id { get; set; }

        [Required]
        public List<int> SelectedBagIds { get; set; } = new();

        [Required]
        public int NewVaultSectionId { get; set; }

        public string? Remarks { get; set; }

        public string? BranchName { get; set; }

        // Select-list / table data for the Create/Edit form
        public List<BagSelectionRowViewModel> Bags { get; set; } = new();
        public List<SelectListItem> VaultSectionList { get; set; } = new();
    }
}
