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

    /// <summary>
    /// Single source of truth for "has this MI &amp; Fit row been declared yet?", shared by the plan
    /// Details view and the ConfirmResults guard so the banner and the block can never disagree.
    /// </summary>
    public static class MIandFitDeclaration
    {
        /// <summary>
        /// A row counts as declared once the board supervisor has entered a return against it.
        /// Both values still sitting at zero is exactly how an untouched row looks, which is the
        /// same rule MIandFitController already uses for its pending count.
        /// </summary>
        public static bool IsDeclared(decimal returnedFitValue, decimal returnedUnFitValue)
            => returnedFitValue != 0 || returnedUnFitValue != 0;
    }

    public class MIandFitViewModel
    {
        public int Id { get; set; }
        public int StockProcessingPlanId { get; set; }
        public int DenominationId { get; set; }
        public string? DenominationName { get; set; }
        public decimal DenominationValue { get; set; }
        public decimal IssuedFitValue { get; set; }
        public decimal IssuedUnFitValue { get; set; }
        public decimal ReturnedFitValue { get; set; }
        public decimal ReturnedUnFitValue { get; set; }
        public string? BoardSupervisorUserId { get; set; }
        public string? BoardSupervisorName { get; set; }

        public bool IsDeclared => MIandFitDeclaration.IsDeclared(ReturnedFitValue, ReturnedUnFitValue);
    }
}
