using Microsoft.EntityFrameworkCore;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;
using Microsoft.Extensions.Logging;

namespace VMSService.Core.Repositories.StockMovementDiscrepancies
{
    public class StockMovementDiscrepancyRepository : IStockMovementDiscrepancyRepository
    {
        private readonly VMSServiceDbContext _context;
        private readonly ILogger<StockMovementDiscrepancyRepository> _logger;

        public StockMovementDiscrepancyRepository(VMSServiceDbContext context, ILogger<StockMovementDiscrepancyRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<StockMovementDiscrepancy>> GetByStockMovementIdAsync(int stockMovementId)
        {
            try
            {
                return await _context.StockMovementDiscrepancies
                    .Where(d => d.StockMovementId == stockMovementId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving discrepancies for stock movement {StockMovementId}", stockMovementId);
                throw;
            }
        }

        public async Task AddAsync(StockMovementDiscrepancy discrepancy)
        {
            try
            {
                _context.StockMovementDiscrepancies.Add(discrepancy);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding discrepancy");
                throw;
            }
        }

        public async Task<StockMovementDiscrepancy> GetByIdAsync(int id)
        {
            try
            {
                return await _context.StockMovementDiscrepancies
                    .Include(d => d.StockMovement)
                        .ThenInclude(m => m.Denominations)
                    .FirstOrDefaultAsync(d => d.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving discrepancy {DiscrepancyId}", id);
                throw;
            }
        }

        public async Task UpdateAsync(StockMovementDiscrepancy discrepancy)
        {
            try
            {
                _context.StockMovementDiscrepancies.Update(discrepancy);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating discrepancy {DiscrepancyId}", discrepancy.Id);
                throw;
            }
        }
    }
}
