using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.PenaltyWaivers
{
    public class PenaltyWaiverRepository(VMSServiceDbContext _db) : IPenaltyWaiverRepository
    {
        public async Task<ICollection<PenaltyWaiver>> FindAll()
            => await _db.PenaltyWaivers
                .Include(w => w.Penalty)
                .OrderByDescending(w => w.StartDate)
                .ToListAsync();

        public async Task<ICollection<PenaltyWaiver>> FindByPenalty(int penaltyId)
            => await _db.PenaltyWaivers
                .Include(w => w.Penalty)
                .Where(w => w.PenaltyId == penaltyId)
                .OrderByDescending(w => w.StartDate)
                .ToListAsync();

        public async Task<PenaltyWaiver> FindById(int id)
            => await _db.PenaltyWaivers.Include(w => w.Penalty).FirstOrDefaultAsync(w => w.Id == id);

        public async Task<bool> IsExists(int id)
            => await _db.PenaltyWaivers.AnyAsync(w => w.Id == id);

        public async Task<bool> Create(PenaltyWaiver entity)
        {
            await _db.PenaltyWaivers.AddAsync(entity);
            return true;
        }

        public Task<bool> Update(PenaltyWaiver entity)
        {
            _db.PenaltyWaivers.Update(entity);
            return Task.FromResult(true);
        }

        public async Task<bool> Save() => await _db.SaveChangesAsync() > 0;

        public async Task<bool> IsWaivedAsync(int penaltyId, int bouBranchId, DateOnly onDate)
            => await FindActiveAsync(penaltyId, bouBranchId, onDate) is not null;

        public async Task<PenaltyWaiver?> FindActiveAsync(int penaltyId, int bouBranchId, DateOnly onDate)
            => await _db.PenaltyWaivers.FirstOrDefaultAsync(w =>
                w.Active &&
                w.PenaltyId == penaltyId &&
                w.BouBranchId == bouBranchId &&
                w.StartDate <= onDate &&
                w.EndDate >= onDate);
    }
}
