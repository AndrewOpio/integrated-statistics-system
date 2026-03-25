using Microsoft.EntityFrameworkCore;
using WorkflowService.Core.Repositories.EntityTypes;
using WorkflowService.Data.Configurations;
using WorkflowService.Data.Entities;

namespace WorkflowService.Core.Repositories.EntityTypes
{
    public class EntityTypeRepository(ApplicationDbContext _db) : IEntityTypeRepository
    {
        public async Task<bool> Create(EntityType model)
        {
            _db.EntityTypes.Add(model);
            return await Save();
        }

        public async Task<ICollection<EntityType>> FindAll()
        {
            return await _db.EntityTypes.ToListAsync();
        }

        public async Task<EntityType> FindById(int id)
        {
            return await _db.EntityTypes
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.EntityTypes
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(EntityType model)
        {
            _db.EntityTypes.Update(model);
            return await Save();
        }

        public async Task<ICollection<EntityType>> FindByApplicationId(int applicationId)
        {
            return await _db.EntityTypes
                .Where(x => x.ApplicationId == applicationId)
                .ToListAsync();
        }

        public async Task<EntityType> FindByNameAndApplicationId(string entityTypeName, int applicationId)
        {
            return await _db.EntityTypes
                .Where(x => x.ApplicationId == applicationId && x.Name == entityTypeName)
                .FirstOrDefaultAsync();
        }
    }
}