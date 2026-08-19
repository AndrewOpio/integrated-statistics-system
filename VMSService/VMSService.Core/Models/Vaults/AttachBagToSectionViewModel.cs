namespace VMSService.Core.Models.Vaults
{
    public class AttachBagToSectionViewModel
    {
        public int VaultSectionId { get; set; }
        public List<int> BagIds { get; set; } = new();
    }
}
