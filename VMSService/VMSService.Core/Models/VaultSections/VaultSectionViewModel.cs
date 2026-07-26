using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using VMSService.Core.Models.Vaults;

namespace VMSService.Core.Models.VaultSections
{
    public class VaultSectionViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int VaultId { get; set; }
        public VaultViewModel? Vault { get; set; }
        public string? VaultName { get; set; }
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public int BagCount { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime ModifiedDate { get; set; }

        public List<SelectListItem> VaultList { get; set; } = new();
        public List<SelectListItem> BranchList { get; set; } = new();

        // All active vaults (with their BranchId) for client-side branch filtering on Create
        public List<VaultViewModel> Vaults { get; set; } = new();
    }
}