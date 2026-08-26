using IdentityService.Data.Entities;

namespace IdentityService.Core.Repository.UserBranches
{
    public interface IUserBranchRepository
    {
        Task<ICollection<UserBranch>> FindAll();
        Task<UserBranch?> FindById(int id);
        Task<ICollection<UserBranch>> FindByUserId(string userId);
        Task<bool> IsExists(string userId, int branchId);
        Task<bool> Create(UserBranch entity);
        Task<bool> Delete(int id);
        Task<bool> Save();
    }
}
