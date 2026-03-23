using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.Logistics
{
    public class LogisticsRepository : ILogisticsRepository
    {
        private readonly VMSServiceDbContext _context;
        private readonly ILogger<LogisticsRepository> _logger;

        public LogisticsRepository(VMSServiceDbContext context, ILogger<LogisticsRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Data.Entities.Logistics>> GetAllAsync()
        {
            return await _context.Logistics
                .AsNoTracking()
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();
        }

        public async Task<Data.Entities.Logistics?> GetByIdAsync(int id)
        {
            return await _context.Logistics
                .Include(f => f.Categories.OrderBy(c => c.SortOrder))
                    .ThenInclude(c => c.Details.OrderBy(d => d.SortOrder))
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<Data.Entities.Logistics> CreateAsync(Data.Entities.Logistics logistics)
        {
            _context.Logistics.Add(logistics);
            await _context.SaveChangesAsync();
            return logistics;
        }

        public async Task UpdateAsync(Data.Entities.Logistics logistics)
        {
            var existingLogistics = await _context.Logistics
                .Include(f => f.Categories)
                    .ThenInclude(c => c.Details)
                .FirstOrDefaultAsync(f => f.Id == logistics.Id);

            if (existingLogistics == null)
                throw new InvalidOperationException("Logistics record not found");

            existingLogistics.Title = logistics.Title;
            existingLogistics.Description = logistics.Description;
            existingLogistics.Date = logistics.Date;
            existingLogistics.ReferenceNumber = logistics.ReferenceNumber;
            existingLogistics.ModifiedDate = DateTime.UtcNow;

            foreach (var category in existingLogistics.Categories.ToList())
            {
                _context.LogisticsDetails.RemoveRange(category.Details);
                _context.LogisticsCategories.Remove(category);
            }

            foreach (var category in logistics.Categories)
            {
                existingLogistics.Categories.Add(category);
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var logistics = await _context.Logistics
                .Include(f => f.Categories)
                    .ThenInclude(c => c.Details)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (logistics != null)
            {
                _context.Logistics.Remove(logistics);
                await _context.SaveChangesAsync();
            }
        }
    }
}
