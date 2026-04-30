using Microsoft.EntityFrameworkCore;
using WorkflowService.Core.Repositories.StatusTypes;
using WorkflowService.Data.Configurations;
using WorkflowService.Data.Entities;

namespace WorkflowService.Core.Repositories.StatusTypes
{
    public class StatusTypeRepository(ApplicationDbContext _db) : IStatusTypeRepository
    {
        public async Task<bool> Create(StatusType model)
        {
            _db.StatusTypes.Add(model);
            return await Save();
        }

        public async Task<ICollection<StatusType>> FindAll()
        {
            return await _db.StatusTypes.Include(st => st.EntityType).ToListAsync();
        }

        public async Task<StatusType> FindById(int id)
        {
            return await _db.StatusTypes
                .Include(st => st.EntityType)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.StatusTypes
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(StatusType model)
        {
            _db.StatusTypes.Update(model);
            return await Save();
        }

        public async Task<StatusType> FindByName(string name, int entityTypeId)
        {
            return await _db.StatusTypes
                .Include(st => st.EntityType)
                .Where(x => x.Name == name && x.EntityTypeId == entityTypeId)
                .FirstOrDefaultAsync();
        }

        public async Task<ICollection<StatusType>> FindByApplicationId(int applicationId)
        {
            return await _db.StatusTypes
                .Include(st => st.EntityType)
                .Where(st => st.EntityType.ApplicationId == applicationId)
                .ToListAsync();
        }
    }
}