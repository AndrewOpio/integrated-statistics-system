using System;
using System.Collections.Generic;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class StockMovement : AuditableEntity
    {
        public int Id { get; set; }
        public string MovementType { get; set; } // "branch" or "internal"
        public DateTime Date { get; set; }
        public string ReferenceNumber { get; set; }

        // Status
        public int? CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; }

        // Branch to Branch
        public int? DispatchingBranchId { get; set; }
        public int? ReceivingBranchId { get; set; }
        public string? Categorization { get; set; } // "buffer" or "operational"

        // Common
        public ICollection<StockMovementDenomination> Denominations { get; set; }
        public ICollection<StockMovementStatusHistory> StatusHistory { get; set; }
        public ICollection<StockMovementDiscrepancy> StockMovementDiscrepancies { get; set; }

        // Totals
        public int? TotalBagsBoxes { get; set; }
        public decimal? GrandTotalValue { get; set; }
    }
}
