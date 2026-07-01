using VMSService.Core.Models.Vaults;
using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repository.Vaults
{
    public interface IVaultRepository : IRepositoryBase<Vault, int>
    {
        Task<List<VaultCardViewModel>> GetVaultDashboardAsync(int? branchId = null);
        Task<bool> DeleteVaultAsync(int id);
    }
}