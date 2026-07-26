using Microsoft.EntityFrameworkCore;
using VMSService.Core.Models.Vaults;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repository.VaultSections
{
    public class VaultSectionRepository(VMSServiceDbContext _db) : IVaultSectionRepository
    {
        public async Task<bool> Create(VaultSection model)
        {
            _db.VaultSections.Add(model);
            return await Save();
        }

        public async Task<ICollection<VaultSection>> FindAll()
        {
            return await _db.VaultSections.Where(x => x.IsDeleted == false)
                .Include(x => x.Vault)
                .ToListAsync();
        }

        public async Task<VaultSection> FindById(int id)
        {
            return await _db.VaultSections
                .Include(x => x.Vault)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.VaultSections
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(VaultSection model)
        {
            _db.VaultSections.Update(model);
            return await Save();
        }

        public async Task<VaultSectionDetailViewModel?> GetSectionDetailAsync(int sectionId)
        {
            var section = await _db.VaultSections
                .Include(s => s.Vault)
                .FirstOrDefaultAsync(s => s.Id == sectionId && !s.IsDeleted);

            if (section == null) return null;

            var bags = await _db.Bags
                .Where(b => b.VaultSectionId == sectionId && !b.IsDeleted && !b.IsProcessed)
                .Include(b => b.BagDenominations.Where(bd => !bd.IsDeleted))
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();

            var bagIds = bags.Select(b => b.Id).ToList();
            var childBundlesByParent = new Dictionary<int, int>();

            if (bagIds.Count > 0)
            {
                var childBagMaps = await _db.BaggedBags
                    .Where(bb => bagIds.Contains(bb.ParentBagId) && !bb.IsDeleted)
                    .Select(bb => new { bb.ParentBagId, bb.ChildBagId })
                    .ToListAsync();

                if (childBagMaps.Count > 0)
                {
                    var childBagIds = childBagMaps.Select(x => x.ChildBagId).Distinct().ToList();
                    var childBundlesByBagId = await _db.BagDenominations
                        .Where(bd => childBagIds.Contains(bd.BagId) && !bd.IsDeleted)
                        .GroupBy(bd => bd.BagId)
                        .Select(g => new { BagId = g.Key, Bundles = g.Sum(bd => bd.NumberOfBundlesOrBoxes) })
                        .ToDictionaryAsync(x => x.BagId, x => x.Bundles);

                    foreach (var map in childBagMaps)
                    {
                        if (!childBundlesByBagId.TryGetValue(map.ChildBagId, out var bundles)) continue;
                        childBundlesByParent.TryGetValue(map.ParentBagId, out var existing);
                        childBundlesByParent[map.ParentBagId] = existing + bundles;
                    }
                }
            }

            return new VaultSectionDetailViewModel
            {
                Id = section.Id,
                Name = section.Name,
                VaultId = section.VaultId,
                VaultName = section.Vault?.Name ?? string.Empty,
                BranchId = section.Vault?.BranchId ?? 0,
                IsActive = section.IsActive,
                Bags = bags.Select(b => new VaultBagViewModel
                {
                    Id = b.Id,
                    ReferenceNumber = b.ReferenceNumber,
                    DenominationId = b.DenominationId,
                    TotalBundles = b.BagDenominations.Sum(bd => bd.NumberOfBundlesOrBoxes)
                                 + childBundlesByParent.GetValueOrDefault(b.Id),
                    IsProcessed = b.IsProcessed,
                }).ToList()
            };
        }

        public async Task<List<VaultBagViewModel>> GetAvailableBagsAsync(int branchId)
        {
            var bags = await _db.Bags
                .Where(b => !b.IsDeleted && !b.IsProcessed && b.VaultSectionId == null && b.BranchId == branchId
                && !_db.BaggedBags.Any(bb => bb.ChildBagId == b.Id && !bb.IsDeleted))
                .Include(b => b.BagDenominations.Where(bd => !bd.IsDeleted))
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();

            // Build child-bundle totals per parent (two flat queries, same pattern as BagRepository)
            var bagIds = bags.Select(b => b.Id).ToList();
            var childBundlesByParent = new Dictionary<int, int>();

            if (bagIds.Count > 0)
            {
                var childBagMaps = await _db.BaggedBags
                    .Where(bb => bagIds.Contains(bb.ParentBagId) && !bb.IsDeleted)
                    .Select(bb => new { bb.ParentBagId, bb.ChildBagId })
                    .ToListAsync();

                if (childBagMaps.Count > 0)
                {
                    var childBagIds = childBagMaps.Select(x => x.ChildBagId).Distinct().ToList();
                    var childBundlesByBagId = await _db.BagDenominations
                        .Where(bd => childBagIds.Contains(bd.BagId) && !bd.IsDeleted)
                        .GroupBy(bd => bd.BagId)
                        .Select(g => new { BagId = g.Key, Bundles = g.Sum(bd => bd.NumberOfBundlesOrBoxes) })
                        .ToDictionaryAsync(x => x.BagId, x => x.Bundles);

                    foreach (var map in childBagMaps)
                    {
                        if (!childBundlesByBagId.TryGetValue(map.ChildBagId, out var bundles)) continue;
                        childBundlesByParent.TryGetValue(map.ParentBagId, out var existing);
                        childBundlesByParent[map.ParentBagId] = existing + bundles;
                    }
                }
            }

            return bags.Select(b => new VaultBagViewModel
            {
                Id = b.Id,
                ReferenceNumber = b.ReferenceNumber,
                DenominationId = b.DenominationId,
                TotalBundles = b.BagDenominations.Sum(bd => bd.NumberOfBundlesOrBoxes)
                             + childBundlesByParent.GetValueOrDefault(b.Id),
                IsProcessed = b.IsProcessed,
            }).ToList();
        }

        public async Task<bool> AttachBagAsync(int bagId, int vaultSectionId)
        {
            var bag = await _db.Bags
                .FirstOrDefaultAsync(b => b.Id == bagId && !b.IsDeleted);

            if (bag == null) return false;

            var sectionExists = await _db.VaultSections
                .AnyAsync(s => s.Id == vaultSectionId && !s.IsDeleted);

            if (!sectionExists) return false;

            bag.VaultSectionId = vaultSectionId;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<int> AttachBagsAsync(List<int> bagIds, int vaultSectionId)
        {
            var section = await _db.VaultSections
                .Include(s => s.Vault)
                .FirstOrDefaultAsync(s => s.Id == vaultSectionId && !s.IsDeleted);

            if (section?.Vault == null) return 0;

            var bags = await _db.Bags
                .Where(b => bagIds.Contains(b.Id) && !b.IsDeleted && b.BranchId == section.Vault.BranchId)
                .ToListAsync();

            foreach (var bag in bags)
                bag.VaultSectionId = vaultSectionId;

            await _db.SaveChangesAsync();
            return bags.Count;
        }

        public async Task<bool> DetachBagAsync(int bagId)
        {
            var bag = await _db.Bags
                .FirstOrDefaultAsync(b => b.Id == bagId && !b.IsDeleted);

            if (bag == null) return false;

            bag.VaultSectionId = null;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSectionAsync(int id)
        {
            var section = await _db.VaultSections
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (section == null) return false;

            var hasBags = await _db.Bags
                .AnyAsync(b => b.VaultSectionId == id && !b.IsDeleted);

            if (hasBags)
                throw new InvalidOperationException("Cannot delete a section that contains bags. Remove all bags first.");

            section.IsDeleted = true;
            await _db.SaveChangesAsync();
            return true;
        }
    }
}