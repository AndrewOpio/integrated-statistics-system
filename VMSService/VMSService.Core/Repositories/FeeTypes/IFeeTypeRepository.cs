using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.FeeTypes
{
    public interface IFeeTypeRepository : IRepositoryBase<FeeType, int>
    {
        Task<FeeType?> FindByName(string name);
    }
}