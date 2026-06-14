using VMSService.Data.Entities;
using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.CfbReports
{
    public class CreateCfbReportDto
    {
        public string ReferenceNumber { get; set; } = null!;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int BranchId { get; set; }
        public string? Remarks { get; set; }
    }

    public class UpdateCfbReportDto
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = null!;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int BranchId { get; set; }
        public string? Remarks { get; set; }
    }

    public class CfbReportDetailsViewModel
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public int CurrentStatusId { get; set; }
        public string CurrentStatusName { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }

        public IEnumerable<CfbReportTransaction> Transactions { get; set; } = [];
        public ICollection<WorkflowTransitionDto> AvailableTransitions { get; set; } = new List<WorkflowTransitionDto>();
        public IEnumerable<CfbReportStatusHistoryViewModel> StatusHistory { get; set; } = [];
    }

    public class CfbReportStatusHistoryViewModel
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
