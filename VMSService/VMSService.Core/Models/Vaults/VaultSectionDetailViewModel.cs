namespace VMSService.Core.Models.Vaults
{
    public class VaultSectionDetailViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int VaultId { get; set; }
        public string VaultName { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public bool IsActive { get; set; }
        public List<VaultBagViewModel> Bags { get; set; } = [];
        public List<VaultBagViewModel> AvailableBags { get; set; } = [];
        public int TotalBags => Bags.Count;
        public int TotalBundles => Bags.Sum(b => b.TotalBundles);
        public decimal TotalAmount => Bags.Sum(b => b.TotalAmount);
    }
}
