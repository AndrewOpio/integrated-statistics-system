namespace VMSService.Core.Models.CountingRoomSessions
{
    public class CountingRoomSessionViewModel
    {
        public int Id { get; set; }
        public int CountingRoomId { get; set; }
        public string? CountingRoomName { get; set; }
        public int StockProcessingPlanId { get; set; }
        public string? BoardSupervisorUserId { get; set; }
        public bool IsDeleted { get; set; }
    }
}
