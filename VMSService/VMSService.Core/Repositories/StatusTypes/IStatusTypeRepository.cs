using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StatusTypes
{
    public interface IStatusTypeRepository : IRepositoryBase<StatusType, int>
    {
        Task<StatusType> FindByName(string name, string category);
    }
}