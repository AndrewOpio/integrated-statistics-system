using System.ComponentModel.DataAnnotations;
using VMSService.Data.Entities;

namespace VMSService.Models
{
    public class PackingListRangeBatchCreateViewModel
    {
        public int ExStockId { get; set; }
        public List<PackingListRangeRowViewModel> Rows { get; set; } = new()
        {
            new PackingListRangeRowViewModel()
        };
    }

    public class PackingListRangeRowViewModel
    {
        [Display(Name = "Denomination")]
        public int? ExPrintMintDetailId { get; set; }

        [Display(Name = "Case Number Prefix")]
        public string? CaseNumberPrefix { get; set; }

        [Display(Name = "Start Case Number")]
        public int? StartCaseNumber { get; set; }

        [Display(Name = "End Case Number")]
        public int? EndCaseNumber { get; set; }

        [Display(Name = "Serial Note Prefix")]
        public string? SNPrefix { get; set; }

        [Display(Name = "SN Range Start")]
        public int? SNRangeStart { get; set; }

        [Display(Name = "SN Range End")]
        public int? SNRangeEnd { get; set; }

        public bool HasAnyValue()
        {
            return ExPrintMintDetailId.HasValue
                || !string.IsNullOrWhiteSpace(CaseNumberPrefix)
                || StartCaseNumber.HasValue
                || EndCaseNumber.HasValue
                || !string.IsNullOrWhiteSpace(SNPrefix)
                || SNRangeStart.HasValue
                || SNRangeEnd.HasValue;
        }

        public PackingListRange ToEntity(int exStockId)
        {
            return new PackingListRange
            {
                ExpectedStockId = exStockId,
                ExPrintMintDetailId = ExPrintMintDetailId ?? 0,
                CaseNumberPrefix = CaseNumberPrefix?.Trim() ?? string.Empty,
                StartCaseNumber = StartCaseNumber ?? 0,
                EndCaseNumber = EndCaseNumber ?? 0,
                SNPrefix = SNPrefix?.Trim() ?? string.Empty,
                SNRangeStart = SNRangeStart ?? 0,
                SNRangeEnd = SNRangeEnd ?? 0
            };
        }
    }
}
