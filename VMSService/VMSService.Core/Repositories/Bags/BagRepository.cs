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
                .Where(x => x.IsDeleted == false)
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

        public async Task<List<BagViewModel>> GetAllBagsAsync()
        {
            return await _db.Bags
                .Where(b => !b.IsDeleted)
                .Include(b => b.BagDenominations)
                    .ThenInclude(bd => bd.DepositRequestDenomination)
                .Select(b => new BagViewModel
                {
                    Id = b.Id,
                    ReferenceNumber = b.ReferenceNumber,
                    DenominationId = b.DenominationId,
                    ItemCount = b.BagDenominations.Count(bd => !bd.IsDeleted),
                    TotalAmount = b.BagDenominations
                        .Where(bd => !bd.IsDeleted)
                        .Sum(bd => bd.DepositRequestDenomination.Amount),
                    IsDeleted = b.IsDeleted,
                    CreatedDate = b.CreatedDate,
                    CreatedBy = b.CreatedBy
                })
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();
        }

        public async Task<BagDetailsViewModel?> GetBagByIdAsync(int bagId)
        {
            var bag = await _db.Bags
                .Where(b => b.Id == bagId && !b.IsDeleted)
                .Include(b => b.BagDenominations.Where(bd => !bd.IsDeleted))
                    .ThenInclude(bd => bd.DepositRequestDenomination)
                        .ThenInclude(d => d.DepositRequest)
                .FirstOrDefaultAsync();

            if (bag == null) return null;

            return new BagDetailsViewModel
            {
                Id = bag.Id,
                ReferenceNumber = bag.ReferenceNumber,
                DenominationId = bag.DenominationId,
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
                        SubCategoryId = bd.DepositRequestDenomination.SubCategoryId
                    })
                    .ToList(),
                TotalAmount = bag.BagDenominations.Sum(bd => bd.DepositRequestDenomination.Amount),
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
                IsDeleted = false
            };

            _db.Bags.Add(bag);
            await _db.SaveChangesAsync();

            return new BagViewModel
            {
                Id = bag.Id,
                ReferenceNumber = bag.ReferenceNumber,
                DenominationId = bag.DenominationId,
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
                    IsInBag = false
                })
                .OrderByDescending(d => d.TransactionRefNumber)
                .ToListAsync();
        }

        public async Task<bool> CreateBagsAsync(List<Bag> bags)
        {
            _db.Bags.AddRange(bags);
            return await Save();
        }
    }
}