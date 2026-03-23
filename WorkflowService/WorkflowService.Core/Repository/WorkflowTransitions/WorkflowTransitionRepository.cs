using Microsoft.EntityFrameworkCore;
using WorkflowService.Data.Configurations;
using WorkflowService.Data.Entities;

namespace WorkflowService.Core.Repositories.WorkflowTransitions
{
    public class WorkflowTransitionRepository(ApplicationDbContext _db) : IWorkflowTransitionRepository
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
                .Include(x => x.EntityType)
                .Include(x => x.FromStatus)
                .Include(x => x.ToStatus)
                .ToListAsync();
        }

        public async Task<WorkflowTransition> FindById(int id)
        {
            return await _db.WorkflowTransitions
                .Include(x => x.EntityType)
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

        public async Task<ICollection<WorkflowTransition>> FindByEntityTypeAndStatus(int entityTypeId, int currentStatusId)
        {
            return await _db.WorkflowTransitions
                .Where(x => x.EntityTypeId == entityTypeId && 
                            x.FromStatusId == currentStatusId && 
                            x.IsDeleted == false && 
                            x.IsActive == true)
                .Include(x => x.EntityType)
                .Include(x => x.FromStatus)
                .Include(x => x.ToStatus)
                .ToListAsync();
        }

        public async Task<WorkflowTransition> FindByTransitionKey(int entityTypeId, int fromStatusId, string actionName)
        {
            return await _db.WorkflowTransitions
                .Where(x => x.EntityTypeId == entityTypeId && 
                            x.FromStatusId == fromStatusId && 
                            x.ActionName == actionName && 
                            x.IsDeleted == false && 
                            x.IsActive == true)
                .Include(x => x.EntityType)
                .Include(x => x.FromStatus)
                .Include(x => x.ToStatus)
                .FirstOrDefaultAsync();
        }

        public async Task<List<WorkflowTransition>> FindByApplicationId(int applicationId)
        {
            return await _db.WorkflowTransitions
                .Include(x => x.EntityType)
                .Include(x => x.FromStatus)
                .Include(x => x.ToStatus)
                .Where(x => x.EntityType.ApplicationId == applicationId && x.IsDeleted == false)
                .ToListAsync();
        }
    }
}