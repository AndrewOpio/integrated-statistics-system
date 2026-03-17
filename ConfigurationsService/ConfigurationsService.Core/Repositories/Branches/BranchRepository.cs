using ConfigurationsService.Data.Configurations;
using ConfigurationsService.Entities;
using Microsoft.EntityFrameworkCore;
using VMSService.Core.Repositories.Branches;

namespace ConfigurationsService.Core.Repositories.Branches
{
    public class BranchRepository(ApplicationDbContext _db) : IBranchRepository
    {
        public async Task<Branch> Create(Branch model)
        {
            // Check for duplicate branch name
            var duplicate = await _db.Branches.AnyAsync(b => b.Name == model.Name && !b.IsDeleted);
            if (duplicate)
                throw new InvalidOperationException("A branch with the same name already exists.");

            _db.Branches.Add(model);
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task<ICollection<Branch>> FindAll()
        {
            return await _db.Branches.Where(x => x.IsDeleted == false).ToListAsync();
        }

        public async Task<Branch> FindById(int id)
        {
            return await _db.Branches.Where(b => !b.IsDeleted)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.Branches.Where(b => !b.IsDeleted)
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(Branch model)
        {
            // Check for duplicate branch name (different id)
            var duplicate = await _db.Branches.AnyAsync(b => b.Name == model.Name && b.Id != model.Id && !b.IsDeleted);
            if (duplicate)
                throw new InvalidOperationException("A branch with the same name already exists.");

            var existing = await _db.Branches.FindAsync(model.Id);
            if (existing == null)
                return false;

            existing.Name = model.Name;
            existing.IsDeleted = model.IsDeleted;
            existing.ModifiedBy = model.ModifiedBy;
            existing.ModifiedDate = model.ModifiedDate;
            // Add other fields as needed
            return await Save();
        }

        public async Task<Branch> FindByName(string branchName)
        {
            return await _db.Branches
                .Where(x => x.Name == branchName).FirstOrDefaultAsync();
        }
    }
}