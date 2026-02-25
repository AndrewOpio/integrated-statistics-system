using VMSService.Core.Models.StatusTypes;

namespace VMSService.Core.Models.ApprovedCurrencyStatusHistories
{
    public class ApprovedCurrencyStatusHistoryViewModel
    {
        public int Id { get; set; }
        public int ApprovedCurrencyProcurementId { get; set; }
        public int OldStatusId { get; set; }
        public StatusTypeViewModel OldStatus {  get; set; }
        public int NewStatusId { get; set; }
        public StatusTypeViewModel NewStatus { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
    }
}