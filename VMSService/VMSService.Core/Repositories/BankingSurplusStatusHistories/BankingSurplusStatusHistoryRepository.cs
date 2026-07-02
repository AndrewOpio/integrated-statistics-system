using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BankingSurplusStatusHistories
{
    public class BankingSurplusStatusHistoryRepository(VMSServiceDbContext _db,
        ILogger<BankingSurplusStatusHistoryRepository> _logger) : IBankingSurplusStatusHistoryRepository
    {
        public async Task<bool> Create(BankingSurplusStatusHistory entity)
        {
            await _db.BankingSurplusStatusHistories.AddAsync(entity);
            return await Save();
        }

        public async Task<ICollection<BankingSurplusStatusHistory>> FindAll()
        {
            return await _db.BankingSurplusStatusHistories.AsNoTracking().ToListAsync();
        }

        public async Task<BankingSurplusStatusHistory> FindById(int id)
        {
            return (await _db.BankingSurplusStatusHistories.FirstOrDefaultAsync(r => r.Id == id))!;
        }

        public async Task<ICollection<BankingSurplusStatusHistory>> FindByBankingSurplusId(int bankingSurplusId)
        {
            return await _db.BankingSurplusStatusHistories
                .AsNoTracking()
                .Where(h => h.BankingSurplusId == bankingSurplusId && !h.IsDeleted)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.BankingSurplusStatusHistories.AnyAsync(r => r.Id == id);
        }

        public async Task<bool> Update(BankingSurplusStatusHistory entity)
        {
            _db.BankingSurplusStatusHistories.Update(entity);
            return await Save();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
