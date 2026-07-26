using ConfigurationsService.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.CfbBoxes
{
    public class CfbBoxRepository(
        VMSServiceDbContext _db,
        IConfigurationsServiceClient _branchClient,
        ILogger<CfbBoxRepository> _logger) : ICfbBoxRepository
    {
        // ── Reads — DB only, no external calls ──────────────────────────────

        public async Task<ICollection<CfbBox>> FindAll()
            => await _db.CfbBoxes
                .Include(b => b.Holdings.Where(h => h.IsActive))
                .ToListAsync();

        public async Task<ICollection<CfbBox>> FindByBranchId(int bouBranchId)
            => await _db.CfbBoxes
                .Where(b => b.BouBranchId == bouBranchId)
                .Include(b => b.Holdings.Where(h => h.IsActive))
                .ToListAsync();

        public async Task<CfbBox?> FindById(int id)
            => await _db.CfbBoxes
                .Include(b => b.Holdings.Where(h => h.IsActive))
                .FirstOrDefaultAsync(b => b.Id == id);

        public async Task<bool> IsExists(int id)
            => await _db.CfbBoxes.AnyAsync(b => b.Id == id);

        // ── Write — validates branch first, then stages ──────────────────────

        public async Task<bool> Create(CfbBox entity)
        {
            BranchDto? branch;
            try
            {
                branch = await _branchClient.GetBouBranchByIdAsync(entity.BouBranchId);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException(
                    $"BOU branch {entity.BouBranchId} does not exist in ConfigurationsService.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "ConfigurationsService unavailable while validating branch {BranchId} for new CfbBox",
                    entity.BouBranchId);
                throw new InvalidOperationException(
                    $"Branch service unavailable — cannot verify branch {entity.BouBranchId} before creating CfbBox.", ex);
            }

            // GetBouBranchByIdAsync returns new BranchDto() (Id == 0) when the body deserialises to null
            if (branch is null || branch.Id == 0)
                throw new InvalidOperationException(
                    $"BOU branch {entity.BouBranchId} was not found in ConfigurationsService.");

            entity.BouBranchCode = branch.BranchCode;
            await _db.CfbBoxes.AddAsync(entity);
            return true;
        }

        public Task<bool> Update(CfbBox entity)
        {
            _db.CfbBoxes.Update(entity);
            return Task.FromResult(true);
        }

        public async Task<bool> Save()
            => await _db.SaveChangesAsync() > 0;
    }
}
