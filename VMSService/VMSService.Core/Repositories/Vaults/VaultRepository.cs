using Microsoft.EntityFrameworkCore;
using VMSService.Core.Models.Vaults;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repository.Vaults
{
    public class VaultRepository(VMSServiceDbContext _db) : IVaultRepository
    {
        public async Task<bool> Create(Vault model)
        {
            _db.Vaults.Add(model);
            return await Save();
        }

        public async Task<ICollection<Vault>> FindAll()
        {
            return await _db.Vaults.Where(x => x.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<Vault> FindById(int id)
        {
            return await _db.Vaults
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.Vaults
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(Vault model)
        {
            _db.Vaults.Update(model);
            return await Save();
        }

        public async Task<bool> DeleteVaultAsync(int id)
        {
            var vault = await _db.Vaults.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
            if (vault == null) return false;

            var sectionIds = await _db.VaultSections
                .Where(s => s.VaultId == id && !s.IsDeleted)
                .Select(s => s.Id)
                .ToListAsync();

            if (sectionIds.Count > 0)
            {
                var hasBags = await _db.Bags
                    .AnyAsync(b => b.VaultSectionId.HasValue
                        && sectionIds.Contains(b.VaultSectionId.Value)
                        && !b.IsDeleted);

                if (hasBags)
                    throw new InvalidOperationException("Cannot delete a vault that contains bags. Remove all bags from its sections first.");

                var sections = await _db.VaultSections
                    .Where(s => s.VaultId == id && !s.IsDeleted)
                    .ToListAsync();

                foreach (var sec in sections)
                    sec.IsDeleted = true;
            }

            vault.IsDeleted = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<VaultCardViewModel>> GetVaultDashboardAsync(int? branchId = null)
        {
            var vaults = await _db.Vaults
                .Where(v => !v.IsDeleted && v.IsActive && (!branchId.HasValue || v.BranchId == branchId.Value))
                .OrderBy(v => v.Name)
                .ToListAsync();

            var sections = await _db.VaultSections
                .Where(s => !s.IsDeleted && s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            var bags = await _db.Bags
                .Where(b => !b.IsDeleted && !b.IsProcessed && b.VaultSectionId.HasValue)
                .Include(b => b.BagDenominations.Where(bd => !bd.IsDeleted))
                .ToListAsync();

            var bagsBySection = bags.GroupBy(b => b.VaultSectionId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var sectionsByVault = sections.GroupBy(s => s.VaultId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return vaults.Select(v => new VaultCardViewModel
            {
                Id = v.Id,
                Name = v.Name,
                BranchId = v.BranchId,
                IsActive = v.IsActive,
                Sections = (sectionsByVault.TryGetValue(v.Id, out var vaultSections) ? vaultSections : [])
                    .Select(s => new VaultSectionCardViewModel
                    {
                        Id = s.Id,
                        Name = s.Name,
                        VaultId = s.VaultId,
                        IsActive = s.IsActive,
                        Bags = (bagsBySection.TryGetValue(s.Id, out var sectionBags) ? sectionBags : [])
                            .Select(b => new VaultBagViewModel
                            {
                                Id = b.Id,
                                ReferenceNumber = b.ReferenceNumber,
                                DenominationId = b.DenominationId,
                                TotalBundles = b.BagDenominations.Sum(bd => bd.NumberOfBundlesOrBoxes),
                                IsProcessed = b.IsProcessed,
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToList();
        }
    }
}