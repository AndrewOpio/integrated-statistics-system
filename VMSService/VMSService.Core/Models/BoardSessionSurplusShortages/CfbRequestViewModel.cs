using System.ComponentModel.DataAnnotations;

namespace VMSService.Core.Models.BoardSessionSurplusShortages
{
    public class CfbRequestViewModel
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        // true = Surplus, false = Shortage
        public bool IsSurplus { get; set; } = true;

        public bool IsDifferentDenomination { get; set; }

        // Hidden / autopopulated fields
        [Required]
        public int StockProcessingSessionId { get; set; }
        public int CommercialBankId { get; set; }
        public int BranchId { get; set; }
        public int DenominationId { get; set; }
        public int CfbBoxId { get; set; }
        public int CurrentStatusId { get; set; }

        // Read-only context shown to the user on the form
        public string? SessionReferenceNumber { get; set; }
        public string? CommercialBankName { get; set; }
        public string? BranchName { get; set; }
        public string? DenominationName { get; set; }
        public decimal DenominationValue { get; set; }
        public decimal TotalIssuedValue { get; set; }
    }
}
