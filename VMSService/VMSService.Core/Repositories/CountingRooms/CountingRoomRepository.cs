using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CountingRooms
{
    public class CountingRoomRepository(VMSServiceDbContext _db) : ICountingRoomRepository
    {
        public async Task<bool> Create(CountingRoom model)
        {
            _db.CountingRooms.Add(model);
            return await Save();
        }

        public async Task<ICollection<CountingRoom>> FindAll()
        {
            return await _db.CountingRooms
                .Where(x => !x.IsDeleted)
                .ToListAsync();
        }

        public async Task<CountingRoom> FindById(int id)
        {
            return await _db.CountingRooms
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.CountingRooms
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(CountingRoom model)
        {
            _db.CountingRooms.Update(model);
            return await Save();
        }
    }
}
