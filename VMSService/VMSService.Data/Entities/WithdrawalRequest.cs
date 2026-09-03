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
        public string? TransactionRefNumber { get; set; } = null;
        public int CbId { get; set; }
        public string CbCode { get; set; } = string.Empty;
        public int BouBranchId { get; set; }
        public string BouBranchCode { get; set; } = string.Empty;
        public DateOnly CollectionDate { get; set; }
        public DateOnly? FundSettlementValueDate { get; set; }
        public string? RtgsCode { get; set; }

        public bool RequiresSpecialApproval { get; set; } = false;
        public string? SpecialApprovalReason { get; set; }

        public int StatusTypeId { get; set; }
        public string StatusTypeName { get; set; } = string.Empty;
        public decimal TotalNotesAmount { get; set; }
        public decimal TotalCoinsAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsLumpSum { get; set; } = false;
        public string RtgsVerificationStatus { get; set; } = "Pending";
        public DateOnly? RtgsVerifiedDate { get; set; }
        public bool RecallRtgsPending { get; set; } = false;
        public string? RecallRtgsCode { get; set; }
        public DateOnly? RecallRtgsDate { get; set; }
        public bool ReturnRtgsPending { get; set; } = false;
        public string? ReturnRtgsCode { get; set; }
        public DateOnly? ReturnRtgsDate { get; set; }
        public string? RtgsCodeAtLastReturn { get; set; }

        public bool FundsNotIssued { get; set; } = false;

        public bool IsDeleted { get; set; } = false;
        public ICollection<WithdrawalRequestAgent> WithdrawalRequestAgents { get; set; } = [];
        public ICollection<WithdrawalRequestDenomination> WithdrawalRequestDenominations { get; set; } = [];
        public ICollection<WithdrawalRequestStatusHistory> WithdrawalRequestStatusHistory { get; set; } = [];
    }
}
