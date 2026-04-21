using VMSService.Core.Models.AgentDetails;

namespace VMSService.Core.Model.AgentDetailStatusHistories
{
    public class AgentDetailStatusHistoryViewModel
    {
        public int Id { get; set; }
        public int AgentDetailId { get; set; }
        public AgentDetailViewModel AgentDetail { get; set; }
        public int OldStatusId { get; set; }
        public string? OldStatusName { get; set; }
        public int NewStatusId { get; set; }
        public string? NewStatusName { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
        public bool IsDeleted { get; set; }
    }
}
