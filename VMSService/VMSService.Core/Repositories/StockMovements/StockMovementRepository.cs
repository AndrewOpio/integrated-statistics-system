using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.StockMovements
{
    public class StockMovementRepository : IStockMovementRepository
    {
        private readonly VMSServiceDbContext _context;
        private readonly ILogger<StockMovementRepository> _logger;

        public StockMovementRepository(VMSServiceDbContext context, ILogger<StockMovementRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<StockMovement> CreateAsync(StockMovement movement)
        {
            await _context.StockMovements.AddAsync(movement);
            await _context.SaveChangesAsync();
            return movement;
        }

        public async Task<StockMovement> FindByIdAsync(int id)
        {
            return await _context.StockMovements
                 .AsNoTracking() // EF won't track changes, less memory overhead
                 .AsSplitQuery()  // Runs separate queries instead of one big JOIN
                .Include(m => m.Denominations)
                    .ThenInclude(d => d.SerialNumberRanges)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<ICollection<StockMovement>> FindAllAsync()
        {
            return await _context.StockMovements
                 .AsNoTracking() // EF won't track changes, less memory overhead
                 .AsSplitQuery()  // Runs separate queries instead of one big JOIN
                .Include(m => m.Denominations)
                    .ThenInclude(d => d.SerialNumberRanges)
                .Include(m => m.StockMovementDiscrepancies)
                .ToListAsync();
        }

        public async Task<bool> UpdateAsync(StockMovement movement)
        {
            _context.StockMovements.Update(movement);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.StockMovements.FindAsync(id);
            if (entity == null) return false;
            _context.StockMovements.Remove(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> SaveAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> IsExistsAsync(int id)
        {
            return await _context.StockMovements.AnyAsync(m => m.Id == id);
        }

        public async Task<bool> SaveReceivedDenominationsAsync(int stockMovementId, List<StockMovementDenomination> receivedDenominations)
        {
            try
            {
                // Add received denominations
                foreach (var denomination in receivedDenominations)
                {
                    denomination.StockMovementId = stockMovementId;
                    denomination.DenominationType = "Received";
                    await _context.StockMovementDenominations.AddAsync(denomination);
                }

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving received denominations for stock movement {StockMovementId}", stockMovementId);
                throw;
            }
        }
    }
}