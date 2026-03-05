using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.ExpectedCurrencies
{
    public interface IExPrintMintRepository : IRepositoryBase<ExPrintMint, int>
    {
        Task<ICollection<ExPrintMint>> FindPendingDeliveries();
    }
}