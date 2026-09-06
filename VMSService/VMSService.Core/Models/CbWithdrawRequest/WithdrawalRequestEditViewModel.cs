using System;

namespace VMSService.Core.Models.CbWithdrawRequest
{
    // Deliberate projection of just the fields Edit.cshtml reads — not the raw WithdrawalRequest
    // EF entity, so the view isn't coupled to the DB schema or EF navigation properties.
    public class WithdrawalRequestEditViewModel
    {
        public Guid Id { get; set; }
        public int BouBranchId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateOnly? FundSettlementValueDate { get; set; }
        public string? RtgsCode { get; set; }
        public bool RequiresSpecialApproval { get; set; }
        public string? SpecialApprovalReason { get; set; }
    }
}
