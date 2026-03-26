using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class WithdrawalRequest : AuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        //BOUBranch-CbCode-W-CollectionDate-SequenceNumber
        public string TraansactionRefNumber { get; set; } = string.Empty;
        public int CbId { get; set; }
        public Guid cbUserId { get; set; }
        public string CbCode { get; set; } = string.Empty;
        public string InitiatorUserId { get; set; } = string.Empty;
        public string? SupervisorUserId { get; set; }
        public int CbBranchId { get; set; }
        public int BouBranchId { get; set; }
        public DateOnly CollectionDate { get; set; }
        public DateOnly? RtgsDate { get; set; }
        public string? RtgsCode { get; set; }

        // ── Special Approval ──────────────────────
        // Triggered when CollectionDate == RtgsDate
        public bool RequiresSpecialApproval { get; set; } = false;
        public string? SpecialApprovalReason { get; set; }
        public string? SpecialApprovalBy { get; set; }
        public DateTime? SpecialApprovalAt { get; set; }

        public int StatusTypeId { get; set; }
        public StatusType StatusType { get; set; } = null!;
        public decimal TotalNotesAmount { get; set; }
        public decimal TotalCoinsAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public ICollection<WithdrawalRequestAgent> WithdrawalRequestAgents { get; set; } = [];
        public ICollection<WithdrawalRequestDenomination> WithdrawalRequestDenominations { get; set; } = [];
        public ICollection<WithdrawalRequestStatusHistory> WithdrawalRequestStatusHistory { get; set; } = [];
    }
}
