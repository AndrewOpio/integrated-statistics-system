using VMSService.Core.Models.CbDepositRequests;
using VMSService.Core.Models.VaultSections;

namespace VMSService.Core.Models.Bags
{
    public class CreateBagViewModel
    {
        public string ReferenceNumber { get; set; } = string.Empty;
        public int DenominationId { get; set; }
        public int BranchId { get; set; }
        public int? VaultSectionId { get; set; }
        public VaultSectionViewModel? VaultSection { get; set; }
        public Guid DepositRequestDenominationId {get; set; }
        public DepositRequestDenominationViewModel? DepositRequestDenomination { get; set; }
        public int BundlesInBag {get; set; }
}
}