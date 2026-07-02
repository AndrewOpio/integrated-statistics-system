namespace VMSService.Core.Models.Vaults
{
    public class VaultBagViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public int DenominationId { get; set; }
        public string? DenominationName { get; set; }
        public decimal DenominationValue { get; set; }
        public int TotalBundles { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsProcessed { get; set; }
        public const int MaxBundles = 50;
        public int FillPercent => MaxBundles > 0
            ? (int)Math.Min(100d, Math.Round((double)TotalBundles / MaxBundles * 100d))
            : 0;
    }
}
