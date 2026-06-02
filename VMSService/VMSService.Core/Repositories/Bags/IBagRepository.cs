using VMSService.Core.Models.Bags;
using VMSService.Core.Models.CbDepositRequests;

namespace VMSService.Core.Repositories.Bags
{
    public interface IBagRepository
    {
        Task<List<BagViewModel>> GetAllBagsAsync();
        Task<BagDetailsViewModel?> GetBagByIdAsync(int bagId);
        Task<BagViewModel?> CreateBagAsync(CreateBagViewModel model);
        Task<bool> AddItemsToBagAsync(AddItemsToBagViewModel model);
        Task<bool> RemoveItemFromBagAsync(int bagId, Guid depositRequestDenominationId);
        Task<bool> DeleteBagAsync(int bagId);
        Task<List<DepositRequestDenominationViewModel>> GetAvailableItemsByDenominationAsync(int denominationId);
    }
}