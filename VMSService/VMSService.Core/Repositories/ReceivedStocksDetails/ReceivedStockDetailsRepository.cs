using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.ReceivedStocksDetails
{
    public class ReceivedStockDetailsRepository(VMSServiceDbContext _db) : IReceivedStockDetailsRepository
    {
        public async Task<bool> Create(ReceivedStockDetails model)
        {
            _db.ReceivedStockDetails.Add(model);
            return await Save();
        }

        public async Task<ICollection<ReceivedStockDetails>> FindAll()
        {
            return await _db.ReceivedStockDetails
                .Where(x => x.IsDeleted == false)
                .Include(x => x.ReceivedStock)
                .Include(x => x.PackingListRange)
                .ToListAsync();
        }

        public async Task<ReceivedStockDetails> FindById(int id)
        {
            return await _db.ReceivedStockDetails
                .Include(x => x.ReceivedStock)
                .Include(x => x.PackingListRange)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.ReceivedStockDetails
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(ReceivedStockDetails model)
        {
            _db.ReceivedStockDetails.Update(model);
            return await Save();
        }

        public async Task<bool> Delete(int id)
        {
            var entity = await _db.ReceivedStockDetails.FindAsync(id);
            if (entity == null)
                return false;

            entity.IsDeleted = true;
            _db.ReceivedStockDetails.Update(entity);
            return await Save();
        }

        public async Task<bool> DeletePermanent(int id)
        {
            var entity = await _db.ReceivedStockDetails.FindAsync(id);
            if (entity == null)
                return false;

            _db.ReceivedStockDetails.Remove(entity);
            return await Save();
        }

        public async Task<int> Count()
        {
            return await _db.ReceivedStockDetails.CountAsync(x => x.IsDeleted == false);
        }
    }
}
