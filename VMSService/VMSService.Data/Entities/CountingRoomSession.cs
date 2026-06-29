using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class CountingRoomSession : AuditableEntity
    {
        public int Id { get; set; }
        public int CountingRoomId { get; set; }
        public CountingRoom? CountingRoom { get; set; }
        public int StockProcessingPlanId { get; set; }
        public StockProcessingPlan? StockProcessingPlan { get; set; }
        public string? BoardSupervisorUserId { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
