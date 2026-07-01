using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.ProcessingBoards
{
    public class ProcessingBoardRepository(VMSServiceDbContext _db) : IProcessingBoardRepository
    {
        public async Task<bool> Create(ProcessingBoard model)
        {
            _db.ProcessingBoards.Add(model);
            return await Save();
        }

        public async Task<ICollection<ProcessingBoard>> FindAll()
        {
            return await _db.ProcessingBoards
                .Where(x => !x.IsDeleted)
                .Include(x => x.BPSMachine)
                .Include(x => x.CountingRoom)
                .ToListAsync();
        }

        public async Task<ProcessingBoard> FindById(int id)
        {
            return await _db.ProcessingBoards
                .Include(x => x.BPSMachine)
                .Include(x => x.CountingRoom)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.ProcessingBoards
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(ProcessingBoard model)
        {
            _db.ProcessingBoards.Update(model);
            return await Save();
        }
    }
}
