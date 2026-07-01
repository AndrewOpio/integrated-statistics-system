namespace VMSService.Core.Models.Bags
{
    public class DepositRequestBagCardViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public decimal DenominationValue { get; set; }
        public string DenominationLabel => $"UGX {DenominationValue:N0}";
        public decimal TotalAmount { get; set; }
        public int TotalBundles { get; set; }
        public int NumberOfBundlesOrBoxes { get; set; }
        public int MaxBundlesPerBag { get; } = 50;
        public int FillPercent => MaxBundlesPerBag > 0
            ? (int)Math.Min(100d, Math.Round((double)NumberOfBundlesOrBoxes / MaxBundlesPerBag * 100d))
            : 0;
        public string Status => NumberOfBundlesOrBoxes >= MaxBundlesPerBag ? "full" : TotalBundles > 0 ? "partial" : "empty";
        public string StatusLabel => Status switch { "full" => "Full", "partial" => "Partial", _ => "Empty" };
        public List<BagItemLineViewModel> Items { get; set; } = [];
    }

    public class BagItemLineViewModel
    {
        public string CbName { get; set; } = string.Empty;
        public DateOnly DepositDate { get; set; }
        public int Bundles { get; set; }
    }
}