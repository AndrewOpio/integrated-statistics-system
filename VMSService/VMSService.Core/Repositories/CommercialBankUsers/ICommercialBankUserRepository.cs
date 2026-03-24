using VMSService.Core.Repository.Common;
using VMSService.Core.Services.Identity.IdentityEntities;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CommercialBankUsers
{
    public interface ICommercialBankUserRepository : IRepositoryBase<CommercialBankUser, int>
    {
        Task<ICollection<NonStaffUser>> GetNonStaffUsers();
        Task<bool> IsUserIdExists(string userId, int? excludeId = null);
        Task<CommercialBankUser> FindByUserId(string userId);
    }
}