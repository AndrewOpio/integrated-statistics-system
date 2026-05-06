using Microsoft.EntityFrameworkCore;
using VMSService.Core.Static;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Repositories.ExpectedCurrencies
{
    public class ExPrintMintRepository(VMSServiceDbContext _db, IWorkflowServiceClient _workflowServiceClient) : IExPrintMintRepository
    {
        public async Task<bool> Create(ExPrintMint model)
        {
            _db.ExPrintMints.Add(model);
            return await Save();
        }

        public async Task<ICollection<ExPrintMint>> FindAll()
        {
            return await _db.ExPrintMints
                .Where(x => x.IsDeleted == false)
                .Include(x => x.CurrencyVendor)
                .Include(x => x.CurrencyVendor.VendorType)
                .Include(x => x.ApprovedCurrencyProcurement)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<ExPrintMint> FindById(int id)
        {
            return await _db.ExPrintMints
                .Include(x => x.CurrencyVendor)
                .Include(x => x.CurrencyVendor.VendorType)
                .Include(x => x.ApprovedCurrencyProcurement)
                .Include(x => x.Details.Where(d => !d.IsDeleted))
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.ExPrintMints
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(ExPrintMint model)
        {
            _db.ExPrintMints.Update(model);
            return await Save();
        }

        public async Task<ICollection<ExPrintMint>> FindPendingDeliveries()
        {
            var verifiedStatus = await _workflowServiceClient.FindStatusByName(new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.ExPrintMint.Verified,
                EntityTypeName = StatusConstants.Entity.ExPrintMint
            });

            if (verifiedStatus == null)
            {
                return new List<ExPrintMint>();
            }

            int verifiedStatusId = verifiedStatus.Id;
            return await _db.ExPrintMints.Where(x => x.CurrentStatusId == verifiedStatusId)
                .Where(x => !x.IsDeleted)
                .Include(x => x.CurrencyVendor)
                .Include(x => x.CurrencyVendor.VendorType)
                .Include(x => x.ApprovedCurrencyProcurement)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<int> CreateNew(ExPrintMint model)
        {
            _db.ExPrintMints.Add(model);
            await Save();
            return model.Id;
        }
    }
}