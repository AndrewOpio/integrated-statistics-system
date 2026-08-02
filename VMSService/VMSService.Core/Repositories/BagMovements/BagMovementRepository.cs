using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.BagMovements
{
    public class BagMovementRepository(VMSServiceDbContext _db) : IBagMovementRepository
    {
        public async Task<bool> Create(BagMovement model)
        {
            _db.BagMovements.Add(model);
            return await Save();
        }

        public async Task<ICollection<BagMovement>> FindAll()
        {
            return await _db.BagMovements
                .Where(x => !x.IsDeleted)
                .Include(x => x.Bag)
                .Include(x => x.OldVaultSection)
                    .ThenInclude(vs => vs!.Vault)
                .Include(x => x.NewVaultSection)
                    .ThenInclude(vs => vs.Vault)
                .Include(x => x.BagMovementBags.Where(mb => !mb.IsDeleted))
                    .ThenInclude(mb => mb.Bag)
                .Include(x => x.BagMovementBags.Where(mb => !mb.IsDeleted))
                    .ThenInclude(mb => mb.OldVaultSection)
                        .ThenInclude(vs => vs!.Vault)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<BagMovement> FindById(int id)
        {
            return await _db.BagMovements
                .Include(x => x.Bag)
                .Include(x => x.OldVaultSection)
                    .ThenInclude(vs => vs!.Vault)
                .Include(x => x.NewVaultSection)
                    .ThenInclude(vs => vs.Vault)
                .Include(x => x.BagMovementBags.Where(mb => !mb.IsDeleted))
                    .ThenInclude(mb => mb.Bag)
                .Include(x => x.BagMovementBags.Where(mb => !mb.IsDeleted))
                    .ThenInclude(mb => mb.OldVaultSection)
                        .ThenInclude(vs => vs!.Vault)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ICollection<BagMovement>> FindByBagIdAsync(int bagId)
        {
            return await _db.BagMovements
                .Where(x => x.BagId == bagId && !x.IsDeleted)
                .Include(x => x.OldVaultSection)
                .Include(x => x.NewVaultSection)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.BagMovements
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> HasPendingForBag(int bagId, int pendingStatusId)
        {
            return await _db.BagMovements
                .AnyAsync(x => !x.IsDeleted && x.CurrentStatusId == pendingStatusId
                    && (x.BagId == bagId || x.BagMovementBags.Any(mb => mb.BagId == bagId && !mb.IsDeleted)));
        }

        public async Task<List<int>> GetBagIdsWithPendingMovement(int pendingStatusId)
        {
            var pendingMovements = _db.BagMovements
                .Where(x => !x.IsDeleted && x.CurrentStatusId == pendingStatusId);

            var legacyBagIds = pendingMovements
                .Where(x => x.BagId != null)
                .Select(x => x.BagId!.Value);

            var childBagIds = pendingMovements
                .SelectMany(x => x.BagMovementBags)
                .Where(mb => !mb.IsDeleted)
                .Select(mb => mb.BagId);

            return await legacyBagIds
                .Union(childBagIds)
                .ToListAsync();
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(BagMovement model)
        {
            _db.BagMovements.Update(model);
            return await Save();
        }
    }
}
