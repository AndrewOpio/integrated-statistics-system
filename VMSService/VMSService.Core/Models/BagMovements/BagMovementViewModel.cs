namespace VMSService.Core.Models.BagMovements
{
    public class BagMovementViewModel
    {
        public int Id { get; set; }
        public int BagId { get; set; }
        public string? BagReferenceNumber { get; set; }
        public int? CurrentStatusId { get; set; }
        public string? CurrentStatusName { get; set; }
        public int OldVaultSectionId { get; set; }
        public string? OldVaultSectionName { get; set; }
        public int NewVaultSectionId { get; set; }
        public string? NewVaultSectionName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
