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
                .Include(x => x.AttachmentRequirements.Where(a => !a.IsDeleted))
                .ToListAsync();
        }

        public async Task<WorkflowTransition> FindById(int id)
        {
            return await _db.WorkflowTransitions
                .Include(x => x.EntityType)
                .Include(x => x.FromStatus)
                .Include(x => x.ToStatus)
                .Include(x => x.AttachmentRequirements.Where(a => !a.IsDeleted))
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
            var existing = await _db.WorkflowTransitions
                .Include(t => t.AttachmentRequirements)
                .FirstOrDefaultAsync(t => t.Id == model.Id);

            if (existing == null) return false;

            _db.Entry(existing).CurrentValues.SetValues(model);

            foreach (var attachment in model.AttachmentRequirements)
            {
                var existingAttachment = existing.AttachmentRequirements
                    .FirstOrDefault(a => a.Id == attachment.Id && attachment.Id != 0);

                if (existingAttachment != null)
                {
                    _db.Entry(existingAttachment).CurrentValues.SetValues(attachment);
                }
                else
                {
                    attachment.WorkflowTransitionId = existing.Id;
                    _db.TransitionAttachmentRequirements.Add(attachment);
                }
            }

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
                .Include(x => x.AttachmentRequirements.Where(a => !a.IsDeleted))
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
                .Include(x => x.AttachmentRequirements.Where(a => !a.IsDeleted))
                .FirstOrDefaultAsync();
        }

        public async Task<List<WorkflowTransition>> FindByApplicationId(int applicationId)
        {
            return await _db.WorkflowTransitions
                .Include(x => x.EntityType)
                .Include(x => x.FromStatus)
                .Include(x => x.ToStatus)
                .Include(x => x.AttachmentRequirements.Where(a => !a.IsDeleted))
                .Where(x => x.EntityType.ApplicationId == applicationId && x.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<ICollection<WorkflowTransition>> FindByEntityTypeStatusAndRole(int entityTypeId, int currentStatusId, int requiredRole)
        {
            return await _db.WorkflowTransitions
                .Where(x => x.EntityTypeId == entityTypeId &&
                            x.FromStatusId == currentStatusId &&
                            x.RequiredRole == requiredRole &&
                            x.IsDeleted == false &&
                            x.IsActive == true)
                .Include(x => x.EntityType)
                .Include(x => x.FromStatus)
                .Include(x => x.ToStatus)
                .Include(x => x.AttachmentRequirements.Where(a => !a.IsDeleted))
                .ToListAsync();
        }
    }
}
