using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class MIandFit : AuditableEntity
    {
        public int Id { get; set; }
        public int StockProcessingPlanId { get; set; }
        public virtual StockProcessingPlan StockProcessingPlan { get; set; }
        public int DenominationId { get; set; }
        public decimal IssuedReIssueValue { get; set; }
        public decimal IssuedCanceledValue { get; set; }
        public decimal ReturnedReIssueValue { get; set; }
        public decimal ReturnedCanceledValue { get; set; }
        public string? BoardSupervisorUserId { get; set; }
        public bool IsDeleted { get; set; }
    }
}
