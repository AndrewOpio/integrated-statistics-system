using Microsoft.EntityFrameworkCore;
using VMSService.Core.Services.Identity;
using VMSService.Core.Services.Identity.IdentityEntities;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CommercialBankUsers
{
    public class CommercialBankUserRepository(VMSServiceDbContext _db, IIdentityService _identityService) : ICommercialBankUserRepository
    {
        public async Task<bool> Create(CommercialBankUser model)
        {
            _db.CommercialBankUsers.Add(model);
            return await Save();
        }

        public async Task<ICollection<CommercialBankUser>> FindAll()
        {
            return await _db.CommercialBankUsers.Where(x => x.IsDeleted == false).ToListAsync();
        }

        public async Task<CommercialBankUser> FindById(int id)
        {
            return await _db.CommercialBankUsers
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.CommercialBankUsers
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> IsUserIdExists(string userId, int? excludeId = null)
        {
            return await _db.CommercialBankUsers
                .AnyAsync(x => x.UserId == userId && x.IsDeleted == false && (excludeId == null || x.Id != excludeId));
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(CommercialBankUser model)
        {
            _db.CommercialBankUsers.Update(model);
            return await Save();
        }

        public async Task<ICollection<NonStaffUser>> GetNonStaffUsers()
        {
            return await _identityService.GetNonStaffUsers();
        }
    }
}