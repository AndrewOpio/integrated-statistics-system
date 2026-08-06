using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class MIandFit : AuditableEntity
    {
        public int Id { get; set; }
        public int StockProcessingPlanId { get; set; }
        public virtual StockProcessingPlan StockProcessingPlan { get; set; }
        public int DenominationId { get; set; }
        public decimal IssuedFitValue { get; set; }
        public decimal IssuedUnFitValue { get; set; }
        public decimal ReturnedFitValue { get; set; }
        public decimal ReturnedUnFitValue { get; set; }
        public string? BoardSupervisorUserId { get; set; }
        public bool IsDeleted { get; set; }
    }
}
