using Microsoft.EntityFrameworkCore;
using WorkflowService.Data.Configurations;
using WorkflowService.Data.Entities;

namespace WorkflowService.Core.Repositories.WorkflowTransitions
{
    public class WorkflowTransitionRepository(ApplicationDbContext _db) : IWorkflowTransitionRepository
    {
        public async Task<bool> Create(WorkflowTransition model)
        {
            // Add the WorkflowTransition along with its AttachmentRequirements
            // EF Core will automatically insert the related AttachmentRequirements
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
            // Load the existing transition with its attachments
            var existingTransition = await _db.WorkflowTransitions
                .Include(t => t.AttachmentRequirements)
                .FirstOrDefaultAsync(t => t.Id == model.Id);

            if (existingTransition == null)
            {
                return false;
            }

            // Update scalar properties
            _db.Entry(existingTransition).CurrentValues.SetValues(model);

            // Handle AttachmentRequirements collection
            // Remove attachments that are no longer in the model
            //var attachmentsToRemove = existingTransition.AttachmentRequirements
            //    .Where(existing => !model.AttachmentRequirements.Any(m => m.Id == existing.Id && existing.Id != 0))
            //    .ToList();

            //foreach (var attachment in attachmentsToRemove)
            //{
            //    _db.TransitionAttachmentRequirements.Remove(attachment);
            //}

            // Update or add attachments
            foreach (var modelAttachment in model.AttachmentRequirements)
            {
                var existingAttachment = existingTransition.AttachmentRequirements
                    .FirstOrDefault(a => a.Id == modelAttachment.Id && modelAttachment.Id != 0);

                if (existingAttachment != null)
                {
                    // Update existing attachment
                    _db.Entry(existingAttachment).CurrentValues.SetValues(modelAttachment);
                }
                else
                {
                    // Add new attachment
                    modelAttachment.WorkflowTransitionId = existingTransition.Id;
                    _db.TransitionAttachmentRequirements.Add(modelAttachment);
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
