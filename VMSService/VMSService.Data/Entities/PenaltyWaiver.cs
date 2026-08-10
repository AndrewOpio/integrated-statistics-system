using System.ComponentModel.DataAnnotations;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class PenaltyWaiver : AuditableEntity
    {
        public int Id { get; set; }

        public int PenaltyId { get; set; }
        public Penalty? Penalty { get; set; }

        /// <summary>External BOU branch ID (ConfigurationsService) — no FK, same pattern as WithdrawalRequest.BouBranchId.</summary>
        public int BouBranchId { get; set; }

        [MaxLength(50)]
        public string BouBranchCode { get; set; } = null!;

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        [MaxLength(500)]
        public string Reason { get; set; } = null!;

        public bool Active { get; set; } = true;
    }
}
