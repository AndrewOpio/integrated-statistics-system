using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.WorkflowTransitions
{
    public class WorkflowTransitionRepository(VMSServiceDbContext _db) : IWorkflowTransitionRepository
    {
        public async Task<bool> Create(WorkflowTransition model)
        {
            _db.WorkflowTransitions.Add(model);
            return await Save();
        }

        public async Task<ICollection<WorkflowTransition>> FindAll()
        {
            return await _db.WorkflowTransitions
                .Where(x => x.IsDeleted == false && x.IsActive == true)
                .Include(x => x.FromStatus)
                .Include(x => x.ToStatus)
                .ToListAsync();
        }

        public async Task<WorkflowTransition> FindById(int id)
        {
            return await _db.WorkflowTransitions
                .Include(x => x.FromStatus)
                .Include(x => x.ToStatus)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.WorkflowTransitions
                .AnyAsync(x => x.Id == id && x.IsDeleted == false);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(WorkflowTransition model)
        {
            _db.WorkflowTransitions.Update(model);
            return await Save();
        }

        public async Task<ICollection<WorkflowTransition>> FindByEntityTypeAndStatus(string entityType, int currentStatusId)
        {
            return await _db.WorkflowTransitions
                .Where(x => x.EntityType == entityType && 
                            x.FromStatusId == currentStatusId && 
                            x.IsDeleted == false && 
                            x.IsActive == true)
                .Include(x => x.FromStatus)
                .Include(x => x.ToStatus)
                .ToListAsync();
        }

        public async Task<WorkflowTransition> FindByTransitionKey(string entityType, int fromStatusId, string actionName)
        {
            return await _db.WorkflowTransitions
                .Where(x => x.EntityType == entityType && 
                            x.FromStatusId == fromStatusId && 
                            x.ActionName == actionName && 
                            x.IsDeleted == false && 
                            x.IsActive == true)
                .Include(x => x.FromStatus)
                .Include(x => x.ToStatus)
                .FirstOrDefaultAsync();
        }
    }
}