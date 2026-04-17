using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VMSService.Data.Entities;
using VMSService.Data.Configurations;

namespace VMSService.Core.Repositories.Logistics
{
    public class LogisticsStatusHistoryRepository : ILogisticsStatusHistoryRepository
    {
        private readonly VMSServiceDbContext _context;
        private readonly ILogger<LogisticsStatusHistoryRepository> _logger;

        public LogisticsStatusHistoryRepository(VMSServiceDbContext context, ILogger<LogisticsStatusHistoryRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<LogisticsStatusHistory>> GetByLogisticsIdAsync(int logisticsId)
        {
            return await _context.LogisticsStatusHistory
                .Include(x => x.OldStatus)
                .Include(x => x.NewStatus)
                .Where(h => h.LogisticsId == logisticsId)
                // .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();
        }

        public async Task CreateAsync(LogisticsStatusHistory statusHistory)
        {
            _context.LogisticsStatusHistory.Add(statusHistory);
            await _context.SaveChangesAsync();
        }
    }
}
