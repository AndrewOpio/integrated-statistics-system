using System.ComponentModel.DataAnnotations;
using VMSService.Core.Models.BoardSessionDifferentDenoms;

namespace VMSService.Core.Models.StockProcessingResults
{
    public class SubmitProcessingResultViewModel
    {
        public int Id { get; set; } // 0 for new, non-zero for edit

        [Required]
        public int SessionId { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Count must be zero or greater.")]
        public int Count { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Value must be zero or greater.")]
        public decimal UnProcessedValue { get; set; } = 0;

        // Machine session fields
        [Range(0, double.MaxValue)]
        public decimal FitValue { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal UnfitShredValue { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal FitShredValue { get; set; } = 0;

        // Manual session fields
        [Range(0, double.MaxValue)]
        public decimal CanceledValue { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal ReissueValue { get; set; } = 0;

        public List<DifferentDenomEntryViewModel> DifferentDenomEntries { get; set; } = [];
    }
}
