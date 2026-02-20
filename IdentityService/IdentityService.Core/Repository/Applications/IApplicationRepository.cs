using IdentityService.Core.Repository.Common;
using IdentityService.Data.Entities;

namespace IdentityService.Core.Repository.Applications
{
    public interface IApplicationRepository : IRepositoryBase<Application, int>
    {
        Task<bool> Delete(Application model);
    }
}
