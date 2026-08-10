namespace VMSService.Core.Models.StockProcessingSessions
{
    public class StockProcessingSessionViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;

        public int StockProcessingPlanId { get; set; }

        public int CommercialBankId { get; set; }
        public string? CommercialBankName { get; set; }

        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }

        public int DenominationId { get; set; }
        public string? DenominationName { get; set; }
        public decimal DenominationValue { get; set; }

        public int? ProcessingBoardId { get; set; }
        public string? ProcessingBoardName { get; set; }
        public string? MachineName { get; set; }

        public int CurrentStatusId { get; set; }
        public string? CurrentStatusName { get; set; }

        public decimal TotalIssuedValue { get; set; }

        public bool IsManual { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsDeleted { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
}
