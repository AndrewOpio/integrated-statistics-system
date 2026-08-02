using VMSService.Core.Services.Identity.IdentityEntities;

namespace VMSService.Core.Models.MIandFits
{
    // Shared shape for the "_MIandFitCard" partial used by both StockProcessingPlan Create and Edit,
    // since those two page models don't share a base type.
    public class MIandFitCardViewModel
    {
        public List<MIandFitViewModel> MIandFits { get; set; } = new();
        public List<UserDto> BoardSupervisors { get; set; } = new();
    }

    public class MIandFitViewModel
    {
        public int Id { get; set; }
        public int StockProcessingPlanId { get; set; }
        public int DenominationId { get; set; }
        public string? DenominationName { get; set; }
        public decimal DenominationValue { get; set; }
        public decimal IssuedReIssueValue { get; set; }
        public decimal IssuedCanceledValue { get; set; }
        public decimal ReturnedReIssueValue { get; set; }
        public decimal ReturnedCanceledValue { get; set; }
        public string? BoardSupervisorUserId { get; set; }
        public string? BoardSupervisorName { get; set; }
    }
}
