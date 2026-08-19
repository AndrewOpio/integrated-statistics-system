namespace VMSService.Core.Models.StockProcessingPlans
{
    /// <summary>
    /// Per-denomination Reissue/Cancelled/Shredded totals for a plan's sessions at ConfirmResults time.
    /// Built from the single latest (most recent) non-deleted StockProcessingResult row per session —
    /// each row is a cumulative snapshot of that session, not an incremental delta, so summing every
    /// row would double-count. Verified rows are preferred when a session has any.
    /// </summary>
    public class PlanConfirmedResultsSummary
    {
        public ICollection<SessionResultDenominationSummary> Denominations { get; set; } = [];
        public decimal TotalReissueValue { get; set; }
        public decimal TotalCanceledValue { get; set; }
        public decimal TotalFitShredValue { get; set; }
    }

    public class SessionResultDenominationSummary
    {
        public int DenominationId { get; set; }
        public decimal ReissueValue { get; set; }
        public decimal CanceledValue { get; set; }
        public decimal FitShredValue { get; set; }
    }
}
