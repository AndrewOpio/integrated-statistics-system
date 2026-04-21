using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.ExPrintMintDetails
{
    public class ExPrintMintDetailRepository(VMSServiceDbContext _db) : IExPrintMintDetailRepository
    {
        public async Task<bool> Create(ExPrintMintDetail model)
        {
            _db.ExPrintMintDetails.Add(model);
            return await Save();
        }

        public async Task<ICollection<ExPrintMintDetail>> FindAll()
        {
            return await _db.ExPrintMintDetails
                .Where(x => x.IsDeleted == false)
                .Include(x => x.ExPrintMint)
                .ToListAsync();
        }

        public async Task<ExPrintMintDetail> FindById(int id)
        {
            return await _db.ExPrintMintDetails
                .Include(x => x.ExPrintMint)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == false);
        }

        public async Task<ICollection<ExPrintMintDetail>> FindByExPrintMintId(int exPrintMintId)
        {
            return await _db.ExPrintMintDetails
                .Where(x => x.ExPrintMintId == exPrintMintId && x.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<ICollection<PackingListRange>> FindPackingListRangesByExPrintMintDetailId(int exPrintMintDetailId)
        {
            return await _db.PackingListRanges
                .Where(x => x.ExPrintMintDetailId == exPrintMintDetailId)
                .ToListAsync();
        }

        public async Task<bool> Update(ExPrintMintDetail model)
        {
            _db.ExPrintMintDetails.Update(model);
            return await Save();
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.ExPrintMintDetails.AnyAsync(x => x.Id == id && x.IsDeleted == false);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
