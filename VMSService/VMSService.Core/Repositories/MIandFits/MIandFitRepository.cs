using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MIandFits
{
    public class MIandFitRepository(VMSServiceDbContext _db) : IMIandFitRepository
    {
        public async Task<bool> Create(MIandFit model)
        {
            _db.MIandFits.Add(model);
            return await Save();
        }

        public async Task<ICollection<MIandFit>> FindAll()
        {
            return await _db.MIandFits
                .Where(x => !x.IsDeleted)
                .ToListAsync();
        }

        public async Task<MIandFit> FindById(int id)
        {
            return await _db.MIandFits
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ICollection<MIandFit>> FindByPlanId(int planId)
        {
            return await _db.MIandFits
                .Where(x => x.StockProcessingPlanId == planId && !x.IsDeleted)
                .ToListAsync();
        }

        public async Task<ICollection<MIandFit>> FindByBoardSupervisor(string userName)
        {
            return await _db.MIandFits
                .Where(x => x.BoardSupervisorUserId == userName && !x.IsDeleted)
                .Include(x => x.StockProcessingPlan)
                .ToListAsync();
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.MIandFits.AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> SaveAll()
        {
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Update(MIandFit model)
        {
            _db.MIandFits.Update(model);
            return await Save();
        }
    }
}
