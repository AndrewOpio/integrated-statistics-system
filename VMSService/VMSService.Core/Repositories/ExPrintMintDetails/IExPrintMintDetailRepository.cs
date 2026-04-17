using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.ExPrintMintDetails
{
    public interface IExPrintMintDetailRepository
    {
        Task<bool> Create(ExPrintMintDetail model);
        Task<ICollection<ExPrintMintDetail>> FindAll();
        Task<ExPrintMintDetail> FindById(int id);
        Task<ICollection<ExPrintMintDetail>> FindByExPrintMintId(int exPrintMintId);
        Task<ICollection<PackingListRange>> FindPackingListRangesByExPrintMintDetailId(int exPrintMintDetailId);
        Task<bool> Update(ExPrintMintDetail model);
        Task<bool> IsExists(int id);
        Task<bool> Save();
    }
}
