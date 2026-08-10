using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlyShortageReimbursements
{
    public interface IMonthlyShortageReimbursementRepository
    {
        Task<List<MonthlyShortageReimbursement>> FindAll();
        Task<MonthlyShortageReimbursement?> FindById(int id);
        Task<bool> ExistsForShortageAndBranch(int monthlyShortageId, int branchId);
        Task<MonthlyShortageReimbursement> Create(MonthlyShortageReimbursement entity);
        Task Update(MonthlyShortageReimbursement entity);
    }
}
