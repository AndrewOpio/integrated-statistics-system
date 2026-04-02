using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.AgentDetailStatusHistories
{
    public interface IAgentDetailStatusHistoryRepository : IRepositoryBase<AgentDetailStatusHistory, int>
    {
        Task<ICollection<AgentDetailStatusHistory>> FindByAgentDetailId(int agentDetailId);
    }
}