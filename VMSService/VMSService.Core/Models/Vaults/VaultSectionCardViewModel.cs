namespace VMSService.Core.Models.Vaults
{
    public class VaultSectionCardViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int VaultId { get; set; }
        public bool IsActive { get; set; }
        public List<VaultBagViewModel> Bags { get; set; } = [];
        public int TotalBags => Bags.Count;
        public int TotalBundles => Bags.Sum(b => b.TotalBundles);
        public decimal TotalAmount => Bags.Sum(b => b.TotalAmount);
    }
}
