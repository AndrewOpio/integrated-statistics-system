using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.ExPrintMintStatusHistories
{
    public interface IExPrintMintStatusHistoryRepository : IRepositoryBase<ExPrintMintStatusHistory, int>
    {
        Task<ICollection<ExPrintMintStatusHistory>> FindByExPrintMintId(int exPrintMintId);
    }
}