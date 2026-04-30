using WorkflowService.Core.Repository.Common;
using WorkflowService.Data.Entities;

namespace WorkflowService.Core.Repositories.StatusTypes
{
    public interface IStatusTypeRepository : IRepositoryBase<StatusType, int>
    {
        Task<StatusType> FindByName(string name, int entityTypeId);
        Task<ICollection<StatusType>> FindByApplicationId(int applicationId);
    }
}