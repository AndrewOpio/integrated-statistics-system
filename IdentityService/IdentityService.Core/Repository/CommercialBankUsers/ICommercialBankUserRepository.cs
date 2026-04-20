using IdentityService.Core.Repository.Common;
using IdentityService.Data.Entities;

namespace IdentityService.Core.Repository.CommercialBankUsers
{
    public interface ICommercialBankUserRepository : IRepositoryBase<CommercialBankUser, int>
    {
        Task<bool> IsUserIdExists(string userId, int? excludeId = null);
        Task<CommercialBankUser> FindByUserId(string userId);
    }
}