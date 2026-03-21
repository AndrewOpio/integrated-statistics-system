using ConfigurationsService.Data.Configurations;
using ConfigurationsService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConfigurationsService.Core.Repositories.CommercialBankBranches
{
    public class CommercialBankBranchRepository(ApplicationDbContext _db) : ICommercialBankBranchRepository
    {
        public async Task<CommercialBankBranch> Create(CommercialBankBranch model)
        {
            // Check for duplicate branch name per bank
            var duplicate = await _db.CommercialBankBranches.AnyAsync(b =>
                b.Name == model.Name && b.CommercialBankId == model.CommercialBankId && !b.IsDeleted);
            if (duplicate)
                throw new InvalidOperationException("A branch with the same name already exists for this bank.");

            _db.CommercialBankBranches.Add(model);
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task<ICollection<CommercialBankBranch>> FindAll()
        {
            return await _db.CommercialBankBranches
                .Where(x => x.IsDeleted == false)
                .Include(x => x.CommercialBank)
                .Include(x => x.District)
                .ToListAsync();
        }

        public async Task<CommercialBankBranch> FindById(int id)
        {
            return await _db.CommercialBankBranches.Where(b => !b.IsDeleted)
                .Include(x => x.CommercialBank)
                .Include(x => x.District)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.CommercialBankBranches.Where(b => !b.IsDeleted)
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(CommercialBankBranch model)
        {
            // Check for duplicate branch name (different id)
            var duplicate = await _db.CommercialBankBranches.AnyAsync(b =>
                b.Name == model.Name && b.CommercialBankId == model.CommercialBankId && b.Id != model.Id && !b.IsDeleted);
            if (duplicate)
                throw new InvalidOperationException("A branch with the same name already exists for this bank.");

            var existing = await _db.CommercialBankBranches.FindAsync(model.Id);
            if (existing == null)
                return false;

            existing.Name = model.Name;
            existing.Location = model.Location;
            existing.CommercialBankId = model.CommercialBankId;
            existing.DistrictId = model.DistrictId;
            existing.IsDeleted = model.IsDeleted;
            existing.ModifiedBy = model.ModifiedBy;
            existing.ModifiedDate = model.ModifiedDate;

            return await Save();
        }

        public async Task<ICollection<CommercialBankBranch>> FindByCommercialBankId(int bankId)
        {
            return await _db.CommercialBankBranches
                .Where(x => x.CommercialBankId == bankId && !x.IsDeleted)
                .Include(x => x.CommercialBank)
                .Include(x => x.District)
                .ToListAsync();
        }

        public async Task<ICollection<CommercialBankBranch>> FindByDistrictId(int districtId)
        {
            return await _db.CommercialBankBranches
                .Where(x => x.DistrictId == districtId && !x.IsDeleted)
                .Include(x => x.CommercialBank)
                .Include(x => x.District)
                .ToListAsync();
        }
    }
}