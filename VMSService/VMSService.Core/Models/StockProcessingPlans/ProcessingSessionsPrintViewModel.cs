namespace VMSService.Core.Models.StockProcessingPlans
{
    public class ProcessingSessionsPrintViewModel
    {
        public int PlanId { get; set; }
        public string PlanReferenceNumber { get; set; } = string.Empty;
        public DateTime ProcessingDate { get; set; }
        public string? BranchName { get; set; }
        public List<PrintDenominationColumn> DenominationColumns { get; set; } = [];
        public List<ProcessingSessionPrintRowViewModel> Rows { get; set; } = [];
    }

    public class PrintDenominationColumn
    {
        public int DenominationId { get; set; }
        public decimal Value { get; set; }
    }

    public class ProcessingSessionPrintRowViewModel
    {
        public int SessionId { get; set; }
        public string WasteNumber { get; set; } = string.Empty;
        public string? CommercialBankName { get; set; }
        public string? SubCategoryName { get; set; }
        public string? BoardSupervisorName { get; set; }

        /// <summary>Total amount for this session, keyed by DenominationId (matches DenominationColumns).</summary>
        public Dictionary<int, decimal> AmountByDenominationId { get; set; } = [];
        public decimal Total { get; set; }
    }
}
