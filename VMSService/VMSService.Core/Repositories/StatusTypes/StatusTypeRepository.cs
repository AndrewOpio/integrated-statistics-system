using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StatusTypes
{
    public class StatusTypeRepository(VMSServiceDbContext _db) : IStatusTypeRepository
    {
        public async Task<bool> Create(StatusType model)
        {
            _db.StatusTypes.Add(model);
            return await Save();
        }

        public async Task<ICollection<StatusType>> FindAll()
        {
            return await _db.StatusTypes.ToListAsync();
        }

        public async Task<StatusType> FindById(int id)
        {
            return await _db.StatusTypes
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

        public async Task<StatusType> FindByName(string name, string category)
        {
            return await _db.StatusTypes
                .Where(x => x.Name == name && x.Category == category)
                .FirstOrDefaultAsync();
        }
    }
}