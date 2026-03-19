using System;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class StockMovementDiscrepancy : AuditableEntity
    {
        public int Id { get; set; }
        
        // Foreign Key to StockMovement
        public int StockMovementId { get; set; }
        
        // Foreign Key to StatusType
        public int CurrentStatusId { get; set; }
        
        // Navigation
        public StockMovement StockMovement { get; set; }
        public StatusType CurrentStatus { get; set; }
    }
}
