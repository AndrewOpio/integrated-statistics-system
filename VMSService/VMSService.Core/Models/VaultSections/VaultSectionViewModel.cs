using VMSService.Core.Models.Vaults;

namespace VMSService.Core.Models.VaultSections
{
    public class VaultSectionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int VaultId { get; set; }
        public VaultViewModel? Vault { get; set; }
        public string? VaultName { get; set; }
        public bool IsDeleted { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public int BagCount { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime ModifiedDate { get; set; }
    }
}