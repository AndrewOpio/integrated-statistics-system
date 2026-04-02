using VMSService.Core.Services.Identity;
using VMSService.Core.Services.Identity.IdentityEntities;
using VMSService.Data.Configurations;

namespace VMSService.Core.Repositories.CommercialBankUsers
{
    public class CommercialBankUserRepository(VMSServiceDbContext _db, IIdentityService _identityService) : ICommercialBankUserRepository
    {
        public async Task<ICollection<NonStaffUser>> GetNonStaffUsers()
        {
            return await _identityService.GetNonStaffUsers();
        }
    }
}