namespace VMSService.Core.Models.Vaults
{
    public class VaultCardViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BranchId { get; set; }
        public bool IsActive { get; set; }
        public List<VaultSectionCardViewModel> Sections { get; set; } = [];
        public int TotalBags => Sections.Sum(s => s.TotalBags);
        public int TotalBundles => Sections.Sum(s => s.TotalBundles);
        public decimal TotalAmount => Sections.Sum(s => s.TotalAmount);
    }
}
