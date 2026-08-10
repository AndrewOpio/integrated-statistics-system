namespace VMSService.Core.Models.StockProcessingSessions
{
    public class StockProcessingSessionIndexItemViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public int StockProcessingPlanId { get; set; }
        public string PlanReferenceNumber { get; set; } = string.Empty;
        public DateTime ProcessingDate { get; set; }
        public string? ProcessingBoardName { get; set; }
        public string? MachineName { get; set; }
        public string? CountingRoomName { get; set; }
        public string? CommercialBankName { get; set; }
        public decimal DenominationValue { get; set; }
        public string? DenominationName { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
        public decimal TotalIssuedValue { get; set; }
        public bool IsManual { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
