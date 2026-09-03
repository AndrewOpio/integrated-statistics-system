using System.ComponentModel.DataAnnotations;
using VMSService.Core.Models.BoardSessionDifferentDenoms;
using VMSService.Core.Models.StockProcessingSessions;

namespace VMSService.Core.Models.StockProcessingResults
{
    public class SubmitProcessingResultViewModel
    {
        public int Id { get; set; } // 0 for new, non-zero for edit

        [Required]
        public int SessionId { get; set; }

        // Machine-only fields
        [Range(0, double.MaxValue)]
        public decimal FitValue { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal UnFitShredValue { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal FitShredValue { get; set; } = 0;

        // Captured for both manual and machine sessions
        [Range(0, double.MaxValue)]
        public decimal CanceledValue { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal ReissueValue { get; set; } = 0;

        /// <summary>
        /// Machine result files the operator picked to import. When any are supplied the machine
        /// fields above are recomputed from the files server-side and the posted values ignored;
        /// leaving this empty keeps the fields as manually keyed.
        /// </summary>
        public List<string> SelectedXmlFiles { get; set; } = [];

        public List<DifferentDenomEntryViewModel> DifferentDenomEntries { get; set; } = [];

        // Manual-only: note examiners who worked this board, and the bundles each processed
        public List<NoteExaminerEntryViewModel> NoteExaminerEntries { get; set; } = [];
    }
}
