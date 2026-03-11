using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.ExPrintMintStatusHistories
{
    public class ExPrintMintStatusHistoryRepository(VMSServiceDbContext _db) : IExPrintMintStatusHistoryRepository
    {
        public async Task<bool> Create(ExPrintMintStatusHistory model)
        {
            _db.ExPrintMintStatusHistory.Add(model);
            return await Save();
        }

        public async Task<ICollection<ExPrintMintStatusHistory>> FindAll()
        {
            return await _db.ExPrintMintStatusHistory.Where(x => x.IsDeleted == false)
                .Include(x => x.ExPrintMint)
                .Include(x => x.OldStatus)
                .Include(x => x.NewStatus)
                .ToListAsync();
        }

        public async Task<ExPrintMintStatusHistory> FindById(int id)
        {
            return await _db.ExPrintMintStatusHistory
                .Include(x => x.ExPrintMint)
                .Include(x => x.OldStatus)
                .Include(x => x.NewStatus)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.ExPrintMintStatusHistory
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(ExPrintMintStatusHistory model)
        {
            _db.ExPrintMintStatusHistory.Update(model);
            return await Save();
        }

        public async Task<ICollection<ExPrintMintStatusHistory>> FindByExPrintMintId(int exPrintMintId)
        {
            return await _db.ExPrintMintStatusHistory.Where(x => x.ExPrintMintId == exPrintMintId && x.IsDeleted == false)
                .Include(x => x.ExPrintMint)
                .Include(x => x.OldStatus)
                .Include(x => x.NewStatus)
                .ToListAsync();
        }
    }
}