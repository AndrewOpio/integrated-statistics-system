using IdentityService.Data.Configurations;
using IdentityService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityService.Core.Repository.Applications
{
    public class ApplicationRepository(ApplicationDbContext _db, ILogger<ApplicationRepository> _logger) : IApplicationRepository
    {
        public async Task<bool> Create(Application model)
        {
            _db.Applications.Add(model);
            return await Save();
        }

        public async Task<ICollection<Application>> FindAll()
        {
            return await _db.Applications.ToListAsync();
        }

        public async Task<Application> FindById(int id)
        {
            var application = await _db.Applications
                .FirstOrDefaultAsync(x => x.Id == id);
            return application;
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.Applications
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(Application model)
        {
            _db.Applications.Update(model);
            return await Save();
        }

        public async Task<bool> Delete(Application model)
        {
            _db.Applications.Remove(model);
            return await Save();
        }
    }
}
