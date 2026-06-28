using System.ComponentModel.DataAnnotations;

namespace VMSService.Core.Models.Vaults
{
    public class CreateVaultViewModel
    {
        [Required(ErrorMessage = "Vault name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        public int BranchId { get; set; }
    }
}
