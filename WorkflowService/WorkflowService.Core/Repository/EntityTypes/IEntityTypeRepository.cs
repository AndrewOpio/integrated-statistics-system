using WorkflowService.Core.Repository.Common;
using WorkflowService.Data.Entities;

namespace WorkflowService.Core.Repositories.EntityTypes
{
    public interface IEntityTypeRepository : IRepositoryBase<EntityType, int>
    {
        Task<ICollection<EntityType>> FindByApplicationId(int applicationId);
        Task<EntityType> FindByNameAndApplicationId(string entityTypeName, int applicationId);
    }
}