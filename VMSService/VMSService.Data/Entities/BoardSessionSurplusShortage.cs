using System.ComponentModel.DataAnnotations.Schema;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class BoardSessionSurplusShortage : AuditableEntity
    {
        public int Id { get; set; }
        public int CommercialBankId { get; set; }
        public int BranchId { get; set; }
        public int? SessionSourceBranch { get; set; }
        public int DenominationId { get; set; }
        public decimal Amount { get; set; }
        public bool IsSurplus { get; set; }
        public bool IsDifferentDenomination { get; set; }
        public bool IsProcessedOnRtgs { get; set; }
        public string? RtgsCode { get; set; }

        public virtual DepositRequest? DepositRequest { get; set; }
        public virtual ICollection<BoardSessionDifferentDenom> DifferentDenoms { get; set; } = [];
        public int StockProcessingSessionId { get; set; }
        public virtual StockProcessingSession? StockProcessingSession { get; set; }
        public int CfbBoxId { get; set; }
        public virtual CfbBox? CfbBox { get; set; }
        public bool IsDeleted { get; set; } = false;
        public int CurrentStatusId { get; set; }
    }
}
