using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.MonthlyShortageReimbursements
{
    public interface IMonthlyShortageReimbursementStatusHistoryRepository
    {
        Task<List<MonthlyShortageReimbursementStatusHistory>> FindByReimbursementId(int reimbursementId);
        Task<MonthlyShortageReimbursementStatusHistory> Create(MonthlyShortageReimbursementStatusHistory entity);
    }
}
