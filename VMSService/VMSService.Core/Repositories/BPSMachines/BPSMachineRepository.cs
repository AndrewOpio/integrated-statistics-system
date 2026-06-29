using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BPSMachines
{
    public class BPSMachineRepository(VMSServiceDbContext _db) : IBPSMachineRepository
    {
        public async Task<bool> Create(BPSMachine model)
        {
            _db.BPSMachines.Add(model);
            return await Save();
        }

        public async Task<ICollection<BPSMachine>> FindAll()
        {
            return await _db.BPSMachines
                .Where(x => !x.IsDeleted)
                .ToListAsync();
        }

        public async Task<BPSMachine> FindById(int id)
        {
            return await _db.BPSMachines
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.BPSMachines
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(BPSMachine model)
        {
            _db.BPSMachines.Update(model);
            return await Save();
        }
    }
}
