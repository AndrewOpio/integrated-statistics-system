using VMSService.Core.Models.Vaults;
using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repository.VaultSections
{
    public interface IVaultSectionRepository : IRepositoryBase<VaultSection, int>
    {
        Task<VaultSectionDetailViewModel?> GetSectionDetailAsync(int sectionId);
        Task<List<VaultBagViewModel>> GetAvailableBagsAsync();
        Task<bool> AttachBagAsync(int bagId, int vaultSectionId);
        Task<bool> DetachBagAsync(int bagId);
        Task<bool> DeleteSectionAsync(int id);
    }
}