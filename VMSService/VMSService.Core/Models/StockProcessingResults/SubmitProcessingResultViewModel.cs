using System.ComponentModel.DataAnnotations;
using VMSService.Core.Models.BoardSessionDifferentDenoms;

namespace VMSService.Core.Models.StockProcessingResults
{
    public class SubmitProcessingResultViewModel
    {
        public int Id { get; set; } // 0 for new, non-zero for edit

        [Required]
        public int SessionId { get; set; }

        // Machine-only field
        [Range(0, double.MaxValue)]
        public decimal FitShredValue { get; set; } = 0;

        // Captured for both manual and machine sessions
        [Range(0, double.MaxValue)]
        public decimal CanceledValue { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal ReissueValue { get; set; } = 0;

        public List<DifferentDenomEntryViewModel> DifferentDenomEntries { get; set; } = [];
    }
}
