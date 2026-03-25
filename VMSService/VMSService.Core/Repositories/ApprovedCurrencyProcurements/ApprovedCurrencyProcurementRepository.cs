using Microsoft.EntityFrameworkCore;
using VMSService.Core.Static;
using VMSService.Data.Configurations;
using VMSService.Data.Entities;
using WorkflowService.Client;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Repositories.ApprovedCurrencyProcurements
{
    public class ApprovedCurrencyProcurementRepository(VMSServiceDbContext _db, IWorkflowServiceClient _workflowServiceClient) : IApprovedCurrencyProcurementRepository
    {
        public async Task<bool> Create(ApprovedCurrencyProcurement model)
        {
            _db.ApprovedCurrencyProcurements.Add(model);
            return await Save();
        }

        public async Task<ICollection<ApprovedCurrencyProcurement>> FindAll()
        {
            return await _db.ApprovedCurrencyProcurements.Where(x => x.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<ApprovedCurrencyProcurement> FindById(int id)
        {
            return await _db.ApprovedCurrencyProcurements
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> IsExists(int id)
        {
            return await _db.ApprovedCurrencyProcurements
                .AnyAsync(x => x.Id == id);
        }

        public async Task<bool> Save()
        {
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(ApprovedCurrencyProcurement model)
        {
            _db.ApprovedCurrencyProcurements.Update(model);
            return await Save();
        }

        public async Task<ICollection<ApprovedCurrencyProcurement>> FindVerified()
        {
            var requestData = new GetStatusByNameRequestDto
            {
                ApplicationAcronym = SystemPolicy.SystemSettings.AppAcronym,
                StatusName = StatusConstants.ApprovedCurrencyProcurement.Verified,
                EntityTypeName = StatusConstants.Entity.ApprovedCurrencyProcurement
            };
            var verifiedStatus = await _workflowServiceClient.FindStatusByName(requestData);

            if (verifiedStatus == null)
            {
                return new List<ApprovedCurrencyProcurement>();
            }

            int verifiedStatusId = verifiedStatus.Id;
            return await _db.ApprovedCurrencyProcurements
                .Where(x => x.IsDeleted == false && x.CurrentStatusId == verifiedStatusId)
                .ToListAsync();
        }

        public async Task<int> CreateNew(ApprovedCurrencyProcurement model)
        {
            _db.ApprovedCurrencyProcurements.Add(model);
            await Save();
            return model.Id;
        }
    }
}
