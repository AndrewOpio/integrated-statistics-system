using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BoardSessionSurplusShortages
{
    public class BoardSessionSurplusShortageRepository(VMSServiceDbContext _db) : IBoardSessionSurplusShortageRepository
    {
        public async Task<bool> Create(BoardSessionSurplusShortage model)
        {
            _db.BoardSessionSurplusShortages.Add(model);
            return await Save();
        }

        public async Task<ICollection<BoardSessionSurplusShortage>> FindAll()
        {
            return await _db.BoardSessionSurplusShortages
                .Where(x => !x.IsDeleted)
                .Include(x => x.StockProcessingSession)
                .Include(x => x.CfbBox)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<ICollection<BoardSessionSurplusShortage>> FindByBranchId(int branchId)
        {
            return await _db.BoardSessionSurplusShortages
                .Where(x => x.BranchId == branchId && !x.IsDeleted)
                .Include(x => x.StockProcessingSession)
                .Include(x => x.CfbBox)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<BoardSessionSurplusShortage> FindById(int id)
        {
            return await _db.BoardSessionSurplusShortages
                .Include(x => x.StockProcessingSession)
                .Include(x => x.CfbBox)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.BoardSessionSurplusShortages
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(BoardSessionSurplusShortage model)
        {
            _db.BoardSessionSurplusShortages.Update(model);
            return await Save();
        }
    }
}
