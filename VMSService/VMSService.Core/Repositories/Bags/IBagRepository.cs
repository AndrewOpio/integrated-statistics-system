using VMSService.Core.Models.Bags;
using VMSService.Core.Models.CbDepositRequests;
using VMSService.Core.Repository.Common;
using VMSService.Data.Entities;

namespace VMSService.Core.Repositories.Bags
{
    public interface IBagRepository : IRepositoryBase<Bag, int>
    {
        Task<List<BagViewModel>> GetAllBagsAsync(int? branchId = null);
        Task<BagDetailsViewModel?> GetBagByIdAsync(int bagId);
        Task<BagViewModel?> CreateBagAsync(CreateBagViewModel model);
        Task<bool> AddItemsToBagAsync(AddItemsToBagViewModel model);
        Task<bool> RemoveItemFromBagAsync(int bagId, Guid depositRequestDenominationId);
        Task<bool> DeleteBagAsync(int bagId);
        Task<List<DepositRequestDenominationViewModel>> GetAvailableItemsByDenominationAsync(int denominationId);
        Task<bool> CreateBagsAsync(List<Bag> bags);
        Task<int> BagSequence(DateTime date, int denominationId, string CbCode);
        Task<List<DepositRequestBagCardViewModel>> GetBagsByDepositRequestIdAsync(Guid depositRequestId);
        Task<int> BagSequence(DateTime date, int denominationId);
        Task<List<NestedBagViewModel>> GetAvailableBagsForMergingAsync(int parentBagId);
        Task<bool> AddBagToBagAsync(AddBagToBagViewModel model);
        Task<bool> RemoveBagFromBagAsync(int baggedBagId);
    }
}