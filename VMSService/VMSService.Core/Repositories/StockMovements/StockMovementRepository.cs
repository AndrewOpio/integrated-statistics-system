using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VMSService.Core.Static;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Repositories.StockMovements
{
    public class StockMovementRepository : IStockMovementRepository
    {
        private readonly VMSServiceDbContext _context;
        private readonly ILogger<StockMovementRepository> _logger;
        private readonly IWorkflowServiceClient _workflowServiceClient;

        public StockMovementRepository(VMSServiceDbContext context, ILogger<StockMovementRepository> logger, IWorkflowServiceClient workflowServiceClient)
        {
            _context = context;
            _logger = logger;
            _workflowServiceClient = workflowServiceClient;
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
                 .AsSplitQuery()  // Runs separate queries instead of one big JOIN
                .Include(m => m.Denominations)
                    .ThenInclude(d => d.BoxNumberRanges)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<StockMovement> FindByIdForEditAsync(int id)
        {
            return await _context.StockMovements
                 .AsSplitQuery()
                .Include(m => m.Denominations)
                    .ThenInclude(d => d.BoxNumberRanges)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<List<StockMovement>> FindByIdsAsync(List<int> branchIds)
        {
            StatusTypeDto dispatchedStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.StockMovement.Dispatched,
                EntityTypeName = StatusConstants.Entity.StockMovement
            });

            StatusTypeDto pendingReceiptStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.StockMovement.PendingReceipt,
                EntityTypeName = StatusConstants.Entity.StockMovement
            });

            StatusTypeDto receivedStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.StockMovement.Received,
                EntityTypeName = StatusConstants.Entity.StockMovement
            });

            return await _context.StockMovements
                .AsNoTracking()
                .AsSplitQuery()
                .Include(m => m.Denominations)
                    .ThenInclude(d => d.BoxNumberRanges)
                .Include(m => m.StockMovementDiscrepancies)
                .Where(m => branchIds.Contains(m.DispatchingBranchId ?? 0) 
                         || (branchIds.Contains(m.ReceivingBranchId ?? 0) 
                             && (m.CurrentStatusId == dispatchedStatus.Id
                                 || m.CurrentStatusId == pendingReceiptStatus.Id
                                 || m.CurrentStatusId == receivedStatus.Id)))
                .ToListAsync();
        }

        public async Task<ICollection<StockMovement>> FindAllAsync()
        {
            return await _context.StockMovements
                 .AsNoTracking() // EF won't track changes, less memory overhead
                 .AsSplitQuery()  // Runs separate queries instead of one big JOIN
                .Include(m => m.Denominations)
                    .ThenInclude(d => d.BoxNumberRanges)
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