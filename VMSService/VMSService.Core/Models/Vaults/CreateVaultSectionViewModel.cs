using System.ComponentModel.DataAnnotations;

namespace VMSService.Core.Models.Vaults
{
    public class CreateVaultSectionViewModel
    {
        [Required]
        public int VaultId { get; set; }

        [Required(ErrorMessage = "Section name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
