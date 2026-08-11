using Microsoft.EntityFrameworkCore;
using VMSService.Core.Models.Bags;
using VMSService.Core.Models.CbDepositRequests;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.Bags
{
    public class BagRepository : IBagRepository
    {
        private readonly VMSServiceDbContext _db;

        public BagRepository(VMSServiceDbContext context)
        {
            _db = context;
        }

        public async Task<bool> Create(Bag model)
        {
            _db.Bags.Add(model);
            return await Save();
        }

        public async Task<ICollection<Bag>> FindAll()
        {
            return await _db.Bags
                .Where(x => !x.IsDeleted && !x.IsProcessed)
                .Include(x => x.VaultSection)
                .Include(x => x.BagDenominations)
                    .ThenInclude(bd => bd.DepositRequestDenomination)
                .ToListAsync();
        }

        public async Task<Bag> FindById(int id)
        {
            return await _db.Bags
                .Include(x => x.VaultSection)
                .Include(x => x.BagDenominations)
                    .ThenInclude(bd => bd.DepositRequestDenomination)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.Bags
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(Bag model)
        {
            _db.Bags.Update(model);
            return await Save();
        }

        public async Task<List<BagViewModel>> GetAllBagsAsync(int? branchId = null)
        {
            var bags = await _db.Bags
                .Where(b => !b.IsDeleted && !b.IsProcessed
                    && (!branchId.HasValue || b.BranchId == branchId.Value)
                    && !_db.BaggedBags.Any(bb => bb.ChildBagId == b.Id && !bb.IsDeleted))
                .Include(b => b.BagDenominations)
                    .ThenInclude(bd => bd.DepositRequestDenomination)
                .Include(b => b.VaultSection)
                    .ThenInclude(vs => vs!.Vault)
                .Select(b => new BagViewModel
                {
                    Id = b.Id,
                    ReferenceNumber = b.ReferenceNumber,
                    DenominationId = b.DenominationId,
                    DenominationValue = b.BagDenominations
                        .Where(bd => !bd.IsDeleted)
                        .Select(bd => bd.DepositRequestDenomination.DenominationValue)
                        .FirstOrDefault(),
                    ItemCount = b.BagDenominations.Count(bd => !bd.IsDeleted),
                    TotalBundles = b.BagDenominations
                        .Where(bd => !bd.IsDeleted)
                        .Sum(bd => bd.NumberOfBundlesOrBoxes),
                    TotalAmount = b.BagDenominations
                        .Where(bd => !bd.IsDeleted)
                        .Sum(bd => bd.DepositRequestDenomination.Amount),
                    IsDeleted = b.IsDeleted,
                    BranchId = b.BranchId,
                    CbCode = b.CbCode,
                    CreatedDate = b.CreatedDate,
                    CreatedBy = b.CreatedBy,
                    VaultSectionId = b.VaultSectionId,
                    VaultSectionName = b.VaultSection != null ? b.VaultSection.Name : null,
                    VaultId = b.VaultSection != null ? (int?)b.VaultSection.VaultId : null,
                    VaultName = b.VaultSection != null ? b.VaultSection.Vault.Name : null,
                    CategoryId = b.CategoryId,
                    SubCategoryId = b.SubCategoryId,
                    Items = b.BagDenominations.ToList(),
                    CommercialBankIds = b.BagDenominations
                        .Where(bd => !bd.IsDeleted)
                        .Select(bd => bd.DepositRequestDenomination.DepositRequest.CbId)
                        .Distinct()
                        .ToList(),
                    HasNestedBags = _db.BaggedBags.Any(bb => bb.ParentBagId == b.Id && !bb.IsDeleted),
                })
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();

            // Merge in child-bag data: CbIds and bundle counts (two flat queries — avoids EF correlated SelectMany issues)
            var bagIds = bags.Select(b => b.Id).ToList();
            if (bagIds.Count > 0)
            {
                // Query 1: parent → child bag id mappings
                var childBagMaps = await _db.BaggedBags
                    .Where(bb => bagIds.Contains(bb.ParentBagId) && !bb.IsDeleted)
                    .Select(bb => new { bb.ParentBagId, bb.ChildBagId })
                    .ToListAsync();

                if (childBagMaps.Count > 0)
                {
                    var childBagIds = childBagMaps.Select(x => x.ChildBagId).Distinct().ToList();

                    // Query 2: denominations for all child bags (flat, no deep navigation)
                    var childDenoms = await _db.BagDenominations
                        .Where(bd => childBagIds.Contains(bd.BagId) && !bd.IsDeleted)
                        .Select(bd => new
                        {
                            bd.BagId,
                            bd.NumberOfBundlesOrBoxes,
                            CbId = bd.DepositRequestDenomination.DepositRequest.CbId
                        })
                        .ToListAsync();

                    // Index child denom data by child bag id
                    var childDataByBagId = childDenoms
                        .GroupBy(x => x.BagId)
                        .ToDictionary(g => g.Key, g => new
                        {
                            CbIds = g.Select(x => x.CbId).Where(id => id > 0).Distinct().ToList(),
                            Bundles = g.Sum(x => x.NumberOfBundlesOrBoxes)
                        });

                    // Build per-parent lookups using the mapping table
                    var childCbIdsByParent = new Dictionary<int, List<int>>();
                    var childBundlesByParent = new Dictionary<int, int>();

                    foreach (var map in childBagMaps)
                    {
                        if (!childDataByBagId.TryGetValue(map.ChildBagId, out var childData)) continue;

                        if (!childCbIdsByParent.ContainsKey(map.ParentBagId))
                            childCbIdsByParent[map.ParentBagId] = [];
                        childCbIdsByParent[map.ParentBagId].AddRange(childData.CbIds);

                        childBundlesByParent.TryGetValue(map.ParentBagId, out var existing);
                        childBundlesByParent[map.ParentBagId] = existing + childData.Bundles;
                    }

                    foreach (var bag in bags)
                    {
                        if (childCbIdsByParent.TryGetValue(bag.Id, out var childCbIds))
                            bag.CommercialBankIds = bag.CommercialBankIds.Union(childCbIds).Distinct().ToList();
                        if (childBundlesByParent.TryGetValue(bag.Id, out var childBundles))
                            bag.TotalBundles += childBundles;
                    }
                }
            }

            return bags;
        }

        public async Task<BagDetailsViewModel?> GetBagByIdAsync(int bagId)
        {
            var bag = await _db.Bags
                .Where(b => b.Id == bagId && !b.IsDeleted && !b.IsProcessed)
                .Include(b => b.BagDenominations.Where(bd => !bd.IsDeleted))
                    .ThenInclude(bd => bd.DepositRequestDenomination)
                        .ThenInclude(d => d.DepositRequest)
                .Include(b => b.VaultSection)
                    .ThenInclude(vs => vs!.Vault)
                .FirstOrDefaultAsync();

            if (bag == null) return null;

            var nestedBags = await _db.BaggedBags
                .Where(bb => bb.ParentBagId == bagId && !bb.IsDeleted)
                .Include(bb => bb.ChildBag)
                    .ThenInclude(cb => cb!.BagDenominations)
                .ToListAsync();

            var nestedBagViewModels = nestedBags.Select(bb => new NestedBagViewModel
            {
                BaggedBagId = bb.Id,
                ChildBagId = bb.ChildBagId,
                ReferenceNumber = bb.ChildBag!.ReferenceNumber,
                TotalBundles = bb.ChildBag.BagDenominations.Where(bd => !bd.IsDeleted).Sum(bd => bd.NumberOfBundlesOrBoxes),
                CreatedDate = bb.ChildBag.CreatedDate,
                CategoryId = bb.ChildBag.CategoryId,
                SubCategoryId = bb.ChildBag.SubCategoryId,
            }).ToList();

            int ownBundles = bag.BagDenominations.Sum(bd => bd.NumberOfBundlesOrBoxes);

            bool isChildBag = await _db.BaggedBags
                .AnyAsync(bb => bb.ChildBagId == bagId && !bb.IsDeleted);

            return new BagDetailsViewModel
            {
                Id = bag.Id,
                ReferenceNumber = bag.ReferenceNumber,
                DenominationId = bag.DenominationId,
                CategoryId = bag.CategoryId,
                SubCategoryId = bag.SubCategoryId,
                IsChildBag = isChildBag,
                VaultSectionName = bag.VaultSection?.Name,
                VaultName = bag.VaultSection?.Vault?.Name,
                Items = bag.BagDenominations
                    .Select(bd => new DepositRequestDenominationViewModel
                    {
                        Id = bd.DepositRequestDenomination.Id,
                        DepositRequestId = bd.DepositRequestDenomination.DepositRequestId,
                        TransactionRefNumber = bd.DepositRequestDenomination.DepositRequest.TransactionRefNumber,
                        DenominationId = bd.DepositRequestDenomination.DenominationId,
                        DenominationValue = bd.DepositRequestDenomination.DenominationValue,
                        DenominationTypeId = bd.DepositRequestDenomination.DenominationTypeId,
                        Bundles = bd.DepositRequestDenomination.Bundles,
                        Boxes = bd.DepositRequestDenomination.Boxes,
                        Amount = bd.DepositRequestDenomination.Amount,
                        Origin = bd.DepositRequestDenomination.Origin,
                        SubCategoryId = bd.DepositRequestDenomination.SubCategoryId,
                        DepositDate = bd.DepositRequestDenomination.DepositRequest.DepositDate,
                        CbId = bd.DepositRequestDenomination.DepositRequest.CbId,
                        NumberOfBundlesOrBoxes = bd.NumberOfBundlesOrBoxes,
                    })
                    .ToList(),
                NestedBags = nestedBagViewModels,
                OwnBundles = ownBundles,
                TotalBundles = ownBundles + nestedBagViewModels.Sum(nb => nb.TotalBundles),
                CreatedDate = bag.CreatedDate,
                CreatedBy = bag.CreatedBy
            };
        }

        public async Task<BagViewModel?> CreateBagAsync(CreateBagViewModel model)
        {
            var bag = new Bag
            {
                ReferenceNumber = model.ReferenceNumber,
                DenominationId = model.DenominationId,
                BranchId = model.BranchId,
                CbCode = model.CbCode,
                IsDeleted = false,
                IsProcessed = false,
            };

            _db.Bags.Add(bag);
            await _db.SaveChangesAsync();

            return new BagViewModel
            {
                Id = bag.Id,
                ReferenceNumber = bag.ReferenceNumber,
                DenominationId = bag.DenominationId,
                BranchId = model.BranchId,
                CbCode = model.CbCode,
                ItemCount = 0,
                TotalAmount = 0,
                IsDeleted = bag.IsDeleted,
                CreatedDate = bag.CreatedDate,
                CreatedBy = bag.CreatedBy
            };
        }

        public async Task<bool> AddItemsToBagAsync(AddItemsToBagViewModel model)
        {
            var bag = await _db.Bags
                .Include(b => b.BagDenominations)
                .FirstOrDefaultAsync(b => b.Id == model.BagId && !b.IsDeleted);

            if (bag == null) return false;

            // Verify all items belong to the same denomination
            var items = await _db.DepositRequestDenominations
                .Where(d => model.DepositRequestDenominationIds.Contains(d.Id))
                .ToListAsync();

            if (items.Any(i => i.DenominationId != bag.DenominationId))
            {
                throw new InvalidOperationException("All items must have the same denomination as the bag.");
            }

            // Check if items are already in another bag
            var existingBagItems = await _db.Set<BagDenomination>()
                .Where(bd => model.DepositRequestDenominationIds.Contains(bd.DepositRequestDenominationId)
                    && !bd.IsDeleted)
                .ToListAsync();

            if (existingBagItems.Any())
            {
                throw new InvalidOperationException("Some items are already in another bag.");
            }

            // Add items to bag
            foreach (var itemId in model.DepositRequestDenominationIds)
            {
                bag.BagDenominations.Add(new BagDenomination
                {
                    BagId = bag.Id,
                    DepositRequestDenominationId = itemId,
                    IsDeleted = false
                });
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveItemFromBagAsync(int bagId, Guid depositRequestDenominationId)
        {
            var bagItem = await _db.Set<BagDenomination>()
                .FirstOrDefaultAsync(bd => bd.BagId == bagId
                    && bd.DepositRequestDenominationId == depositRequestDenominationId
                    && !bd.IsDeleted);

            if (bagItem == null) return false;

            bagItem.IsDeleted = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteBagAsync(int bagId)
        {
            var bag = await _db.Bags
                .Include(b => b.BagDenominations)
                .FirstOrDefaultAsync(b => b.Id == bagId && !b.IsDeleted);

            if (bag == null) return false;

            bag.IsDeleted = true;
            foreach (var item in bag.BagDenominations)
            {
                item.IsDeleted = true;
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<DepositRequestDenominationViewModel>> GetAvailableItemsByDenominationAsync(int denominationId)
        {
            var bagged = await _db.Set<BagDenomination>()
                .Where(bd => !bd.IsDeleted)
                .Select(bd => bd.DepositRequestDenominationId)
                .ToListAsync();

            return await _db.DepositRequestDenominations
                .Where(d => d.DenominationId == denominationId && !bagged.Contains(d.Id))
                .Include(d => d.DepositRequest)
                .Select(d => new DepositRequestDenominationViewModel
                {
                    Id = d.Id,
                    DepositRequestId = d.DepositRequestId,
                    TransactionRefNumber = d.DepositRequest.TransactionRefNumber,
                    DenominationId = d.DenominationId,
                    DenominationValue = d.DenominationValue,
                    DenominationTypeId = d.DenominationTypeId,
                    Bundles = d.Bundles,
                    Boxes = d.Boxes,
                    Amount = d.Amount,
                    Origin = d.Origin,
                    SubCategoryId = d.SubCategoryId,
                    IsInBag = false,
                    DepositDate = d.DepositRequest.DepositDate
                })
                .OrderByDescending(d => d.TransactionRefNumber)
                .ToListAsync();
        }

        public async Task<bool> CreateBagsAsync(List<Bag> bags)
        {
            _db.Bags.AddRange(bags);
            return await Save();
        }

        public async Task<int> BagSequence(DateTime date, int denominationId, string CbCode)
        {
            var bags = await _db.Bags
                .Where(b => b.DenominationId == denominationId && b.CbCode == CbCode)
                .Where(b => !b.IsDeleted)
                .Where(b => b.CreatedDate.Date == date.Date)
                .ToListAsync();
            return bags.Count + 1;
        }

        public async Task<List<DepositRequestBagCardViewModel>> GetBagsByDepositRequestIdAsync(Guid depositRequestId)
        {
            var bags = await _db.Bags
                .Where(b => !b.IsDeleted &&
                    b.BagDenominations.Any(bd => !bd.IsDeleted &&
                        bd.DepositRequestDenomination.DepositRequestId == depositRequestId))
                .Include(b => b.BagDenominations.Where(bd => !bd.IsDeleted))
                    .ThenInclude(bd => bd.DepositRequestDenomination)
                        .ThenInclude(d => d.DepositRequest)
                .OrderBy(b => b.ReferenceNumber)
                .ToListAsync();

            return [.. bags.Select(b =>
            {
                var activeItems = b.BagDenominations.Where(bd => !bd.IsDeleted).ToList();
                return new DepositRequestBagCardViewModel
                {
                    Id = b.Id,
                    ReferenceNumber = b.ReferenceNumber,
                    DenominationValue = activeItems
                        .Select(bd => bd.DepositRequestDenomination.DenominationValue)
                        .FirstOrDefault(),
                    TotalAmount = activeItems.Sum(bd => bd.DepositRequestDenomination.Amount),
                    NumberOfBundlesOrBoxes = activeItems.Sum(bd => bd.NumberOfBundlesOrBoxes),
                    TotalBundles = activeItems.Sum(bd =>
                        bd.DepositRequestDenomination.Bundles ?? bd.DepositRequestDenomination.Boxes ?? 0),
                    Items = activeItems.Select(bd => new BagItemLineViewModel
                    {
                        CbName = bd.DepositRequestDenomination.DepositRequest.CbCode ?? string.Empty,
                        DepositDate = bd.DepositRequestDenomination.DepositRequest.DepositDate,
                        Bundles = bd.NumberOfBundlesOrBoxes
                    }).ToList()
                };
            })];
        }

        public async Task<int> BagSequence(DateTime date, int denominationId)
        {
            var bags = await _db.Bags
                .Where(b => b.DenominationId == denominationId && b.CbCode == null)
                .Where(b => !b.IsDeleted)
                .Where(b => b.CreatedDate.Date == date.Date)
                .ToListAsync();
            return bags.Count + 1;
        }

        public async Task<List<NestedBagViewModel>> GetAvailableBagsForMergingAsync(int parentBagId)
        {
            var parentBag = await _db.Bags
                .Include(b => b.BagDenominations.Where(bd => !bd.IsDeleted))
                .FirstOrDefaultAsync(b => b.Id == parentBagId && !b.IsDeleted);

            if (parentBag == null) return [];

            var alreadyChildBagIds = await _db.BaggedBags
                .Where(bb => !bb.IsDeleted)
                .Select(bb => bb.ChildBagId)
                .ToListAsync();

            var existingChildBagIds = await _db.BaggedBags
                .Where(bb => bb.ParentBagId == parentBagId && !bb.IsDeleted)
                .Select(bb => bb.ChildBagId)
                .ToListAsync();

            int parentOwnBundles = parentBag.BagDenominations.Sum(bd => bd.NumberOfBundlesOrBoxes);

            int existingChildBundleSum = existingChildBagIds.Count > 0
                ? await _db.BagDenominations
                    .Where(bd => existingChildBagIds.Contains(bd.BagId) && !bd.IsDeleted)
                    .SumAsync(bd => (int?)bd.NumberOfBundlesOrBoxes) ?? 0
                : 0;

            int remainingCapacity = 50 - parentOwnBundles - existingChildBundleSum;
            if (remainingCapacity <= 0) return [];

            var candidates = await _db.Bags
                .Where(b => !b.IsDeleted && !b.IsProcessed
                    && b.Id != parentBagId
                    && b.DenominationId == parentBag.DenominationId
                    && !alreadyChildBagIds.Contains(b.Id))
                .Include(b => b.BagDenominations.Where(bd => !bd.IsDeleted))
                .ToListAsync();

            return candidates
                .Select(b => new { Bag = b, Bundles = b.BagDenominations.Sum(bd => bd.NumberOfBundlesOrBoxes) })
                .Where(x => x.Bundles < 50 && x.Bundles <= remainingCapacity)
                .Select(x => new NestedBagViewModel
                {
                    ChildBagId = x.Bag.Id,
                    ReferenceNumber = x.Bag.ReferenceNumber,
                    TotalBundles = x.Bundles,
                    CreatedDate = x.Bag.CreatedDate,
                })
                .ToList();
        }

        public async Task<bool> AddBagToBagAsync(AddBagToBagViewModel model, int? mixedSubCategoryId)
        {
            var parentBag = await _db.Bags
                .Include(b => b.BagDenominations.Where(bd => !bd.IsDeleted))
                .FirstOrDefaultAsync(b => b.Id == model.ParentBagId && !b.IsDeleted);

            var childBag = await _db.Bags
                .Include(b => b.BagDenominations.Where(bd => !bd.IsDeleted))
                .FirstOrDefaultAsync(b => b.Id == model.ChildBagId && !b.IsDeleted);

            if (parentBag == null || childBag == null) return false;

            if (parentBag.DenominationId != childBag.DenominationId)
                throw new InvalidOperationException("Both bags must have the same denomination.");

            var alreadyNested = await _db.BaggedBags
                .AnyAsync(bb => bb.ChildBagId == model.ChildBagId && !bb.IsDeleted);

            if (alreadyNested)
                throw new InvalidOperationException("This bag is already nested inside another bag.");

            int childBundles = childBag.BagDenominations.Sum(bd => bd.NumberOfBundlesOrBoxes);

            if (childBundles >= 50)
                throw new InvalidOperationException("The child bag must have fewer than 50 bundles.");

            int parentOwnBundles = parentBag.BagDenominations.Sum(bd => bd.NumberOfBundlesOrBoxes);

            var existingChildBagIds = await _db.BaggedBags
                .Where(bb => bb.ParentBagId == model.ParentBagId && !bb.IsDeleted)
                .Select(bb => bb.ChildBagId)
                .ToListAsync();

            int existingChildBundleSum = existingChildBagIds.Count > 0
                ? await _db.BagDenominations
                    .Where(bd => existingChildBagIds.Contains(bd.BagId) && !bd.IsDeleted)
                    .SumAsync(bd => (int?)bd.NumberOfBundlesOrBoxes) ?? 0
                : 0;

            if (parentOwnBundles + existingChildBundleSum + childBundles > 50)
                throw new InvalidOperationException($"Adding this bag would exceed the maximum of 50 bundles for the parent bag.");

            _db.BaggedBags.Add(new BaggedBag
            {
                ParentBagId = model.ParentBagId,
                ChildBagId = model.ChildBagId,
                IsDeleted = false,
            });

            // Update parent bag category/subcategory
            parentBag.CategoryId = childBag.CategoryId;

            var existingChildSubCategoryIds = existingChildBagIds.Count > 0
                ? await _db.Bags
                    .Where(b => existingChildBagIds.Contains(b.Id) && !b.IsDeleted)
                    .Select(b => b.SubCategoryId)
                    .ToListAsync()
                : new List<int?>();

            var allSubCategoryIds = existingChildSubCategoryIds
                .Append(childBag.SubCategoryId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            parentBag.SubCategoryId = allSubCategoryIds.Count == 1
                ? allSubCategoryIds[0]
                : mixedSubCategoryId;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveBagFromBagAsync(int baggedBagId)
        {
            var baggedBag = await _db.BaggedBags
                .FirstOrDefaultAsync(bb => bb.Id == baggedBagId && !bb.IsDeleted);

            if (baggedBag == null) return false;

            baggedBag.IsDeleted = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<Bag>> GetProcessedBagsByDenominationAndBranchAsync(int denominationId, int branchId, int? categoryId = null)
        {
            return await _db.Bags
                .Where(b => b.DenominationId == denominationId
                         //&& b.BranchId == branchId
                         && b.VaultSection.Vault.BranchId == branchId
                         && b.IsProcessed
                         && !b.IsDeleted
                         && (!categoryId.HasValue || b.CategoryId == categoryId))
                .OrderBy(b => b.ReferenceNumber)
                .ToListAsync();
        }

        public async Task<List<Bag>> GetBagsByIdsAsync(List<int> bagIds)
        {
            return await _db.Bags
                .Where(b => bagIds.Contains(b.Id) && !b.IsDeleted)
                .Select(b => new Bag { Id = b.Id, ReferenceNumber = b.ReferenceNumber })
                .ToListAsync();
        }
    }
}