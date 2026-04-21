namespace VMSService.Core.Models.CbDepositRequests
{
    public class DepositStepViewModel
    {
        public IReadOnlyList<DepositStep> Steps { get; init; } = [];
        public bool IsInProgress { get; init; }  // request is with the BOU
        public bool IsApproved { get; init; }    // request fully approved
        public bool ShowBouPill { get; init; }
    }
}
