using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BoardSessionSurplusShortageStatusHistories
{
    public class BoardSessionSurplusShortageStatusHistoryRepository(VMSServiceDbContext _db,
        ILogger<BoardSessionSurplusShortageStatusHistoryRepository> _logger) : IBoardSessionSurplusShortageStatusHistoryRepository
    {
        public async Task<bool> Create(BoardSessionSurplusShortageStatusHistory entity)
        {
            await _db.BoardSessionSurplusShortageStatusHistory.AddAsync(entity);
            return await Save();
        }

        public async Task<ICollection<BoardSessionSurplusShortageStatusHistory>> FindAll()
        {
            return await _db.BoardSessionSurplusShortageStatusHistory.AsNoTracking().ToListAsync();
        }

        public async Task<BoardSessionSurplusShortageStatusHistory> FindById(int id)
        {
            return (await _db.BoardSessionSurplusShortageStatusHistory.FirstOrDefaultAsync(r => r.Id == id))!;
        }

        public async Task<ICollection<BoardSessionSurplusShortageStatusHistory>> FindByBoardSessionSurplusShortageId(int boardSessionSurplusShortageId)
        {
            return await _db.BoardSessionSurplusShortageStatusHistory
                .AsNoTracking()
                .Where(h => h.BoardSessionSurplusShortageId == boardSessionSurplusShortageId && !h.IsDeleted)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.BoardSessionSurplusShortageStatusHistory.AnyAsync(r => r.Id == id);
        }

        public async Task<bool> Update(BoardSessionSurplusShortageStatusHistory entity)
        {
            _db.BoardSessionSurplusShortageStatusHistory.Update(entity);
            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
