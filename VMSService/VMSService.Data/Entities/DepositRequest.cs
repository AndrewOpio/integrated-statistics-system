using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class DepositRequest : AuditableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        //BOUBranch-CbCode-D-DepositDate-SequenceNumber
        public string TraansactionRefNumber { get; set; } = string.Empty;
        public int CbId { get; set; }
        public Guid cbUserId { get; set; }
        public string CbCode { get; set; } = string.Empty;
        public string InitiatorUserId { get; set; } = string.Empty;
        public string? SupervisorUserId { get; set; }
        public int CbBranchId { get; set; }
        public int BouBranchId { get; set; }
        public DateOnly DepositDate { get; set; }
        public DateOnly? RtgsDate { get; set; }
        public string? RtgsCode { get; set; }

        public int StatusTypeId { get; set; }
        public decimal TotalNotesAmount { get; set; }
        public decimal TotalCoinsAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public ICollection<DepositRequestAgent> DepositRequestAgents { get; set; } = [];
        public ICollection<DepositRequestDenomination> DepositRequestDenominations { get; set; } = [];
        public ICollection<DepositRequestStatusHistory> DepositRequestStatusHistory { get; set; } = [];
    }
}
