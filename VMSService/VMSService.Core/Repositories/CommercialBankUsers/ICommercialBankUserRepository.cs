using VMSService.Core.Services.Identity.IdentityEntities;

namespace VMSService.Core.Repositories.CommercialBankUsers
{
    public interface ICommercialBankUserRepository
    {
        Task<ICollection<NonStaffUser>> GetNonStaffUsers();
    }
}