using VMSService.Data.Entities;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.EodDiscrepancyReports
{
    public class CreateEodDiscrepancyReportDto
    {
        public string ReferenceNumber { get; set; } = null!;
        public DateTime SessionDate { get; set; }
        public int BranchId { get; set; }
        public string? Remarks { get; set; }
    }

    public class UpdateEodDiscrepancyReportDto
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = null!;
        public DateTime SessionDate { get; set; }
        public int BranchId { get; set; }
        public string? Remarks { get; set; }
    }

    public class EodDiscrepancyReportDetailsViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public DateTime SessionDate { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }

        public IEnumerable<BoardSessionSurplusShortage> Transactions { get; set; } = [];
        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; } = new List<WorkflowTransitionDto>();
        public IEnumerable<EodDiscrepancyReportStatusHistoryViewModel> StatusHistory { get; set; } = [];
    }

    public class EodDiscrepancyReportStatusHistoryViewModel
    {
        public int OldStatusId { get; set; }
        public string OldStatusName { get; set; } = string.Empty;
        public int NewStatusId { get; set; }
        public string NewStatusName { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string? Remarks { get; set; }
    }
}
