namespace VMSService.Core.Models.BagMovements
{
    public class BagMovementViewModel
    {
        public int Id { get; set; }
        public List<BagSelectionRowViewModel> Bags { get; set; } = new();
        public int BagCount => Bags.Count;
        public string BagSummary => Bags.Count == 0
            ? "-"
            : Bags.Count == 1
                ? Bags[0].ReferenceNumber
                : $"{Bags[0].ReferenceNumber} +{Bags.Count - 1} more";
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
        public int? CurrentStatusId { get; set; }
        public string? CurrentStatusName { get; set; }
        public int NewVaultSectionId { get; set; }
        public string? NewVaultSectionName { get; set; }
        public string? NewVaultName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
