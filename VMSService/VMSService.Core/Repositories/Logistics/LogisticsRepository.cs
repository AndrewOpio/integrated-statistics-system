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
            existingLogistics.TeamLeader = logistics.TeamLeader;
            existingLogistics.AssistantTeamLeader = logistics.AssistantTeamLeader;
            existingLogistics.ModifiedDate = DateTime.UtcNow;
            existingLogistics.CurrentStatusId = logistics.CurrentStatusId;

            // Sync categories
            var incomingCategoryIds = logistics.Categories.Select(c => c.Id).ToHashSet();
            var existingCategoryIds = existingLogistics.Categories.Select(c => c.Id).ToHashSet();

            // Remove categories not in the incoming list
            foreach (var category in existingLogistics.Categories.ToList())
            {
                if (!incomingCategoryIds.Contains(category.Id))
                {
                    _context.LogisticsDetails.RemoveRange(category.Details);
                    _context.LogisticsCategories.Remove(category);
                }
            }

            // Update or add categories
            foreach (var incomingCat in logistics.Categories)
            {
                var existingCat = existingLogistics.Categories.FirstOrDefault(c => c.Id == incomingCat.Id);
                if (existingCat != null)
                {
                    // Update fields
                    existingCat.Name = incomingCat.Name;
                    existingCat.SortOrder = incomingCat.SortOrder;

                    // Sync details
                    var incomingDetailIds = incomingCat.Details.Select(d => d.Id).ToHashSet();
                    var existingDetailIds = existingCat.Details.Select(d => d.Id).ToHashSet();

                    // Remove details not in the incoming list
                    foreach (var detail in existingCat.Details.ToList())
                    {
                        if (!incomingDetailIds.Contains(detail.Id))
                        {
                            _context.LogisticsDetails.Remove(detail);
                        }
                    }

                    // Update or add details
                    foreach (var incomingDetail in incomingCat.Details)
                    {
                        var existingDetail = existingCat.Details.FirstOrDefault(d => d.Id == incomingDetail.Id);
                        if (existingDetail != null)
                        {
                            existingDetail.ItemName = incomingDetail.ItemName;
                            existingDetail.Description = incomingDetail.Description;
                            existingDetail.UnitCost = incomingDetail.UnitCost;
                            existingDetail.Quantity = incomingDetail.Quantity;
                            existingDetail.Frequency = incomingDetail.Frequency;
                            existingDetail.TotalCost = incomingDetail.TotalCost;
                            existingDetail.Notes = incomingDetail.Notes;
                            existingDetail.SortOrder = incomingDetail.SortOrder;
                        }
                        else
                        {
                            // Add new detail
                            existingCat.Details.Add(incomingDetail);
                        }
                    }
                }
                else
                {
                    // Add new category (with details)
                    existingLogistics.Categories.Add(incomingCat);
                }
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
