namespace VMSService.Core.Models.WithdrawalRequestDenominations
{
    public class DispatchedBoxDetail
    {
        public string CaseNumber { get; set; } = "";
        public int BundlesTaken { get; set; }
        public long SerialStartTaken { get; set; }
        public long SerialEndTaken { get; set; }
        public string SerialPrefix { get; set; } = "";
    }
}
