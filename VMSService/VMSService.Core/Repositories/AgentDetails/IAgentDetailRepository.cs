using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.AgentDetails
{
    public interface IAgentDetailRepository : IRepositoryBase<AgentDetail, int>
    {
        Task<int> CreateNew(AgentDetail model);
    }
}