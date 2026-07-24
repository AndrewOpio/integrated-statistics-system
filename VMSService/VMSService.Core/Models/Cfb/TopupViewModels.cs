using WorkflowService.Client.Dto;

namespace VMSService.Core.Models.Cfb
{
    // ── List ────────────────────────────────────────────────────────────────

    public class TopupListViewModel
    {
        public ICollection<TopupListRowViewModel> Rows { get; init; } = [];
        public bool IsCfbSupervisor { get; init; }
    }

    public class TopupListRowViewModel
    {
        public int Id { get; init; }
        public string StatusName { get; init; } = string.Empty;
        public string CreatedBy { get; init; } = string.Empty;
        public DateTime CreatedDate { get; init; }
        public long TotalAmount { get; init; }
    }

    // ── Create ──────────────────────────────────────────────────────────────

    public class TopupCreateViewModel
    {
        public int BouBranchId { get; init; }
        public string BouBranchName { get; init; } = string.Empty;
        public List<TopupBoxSectionViewModel> Boxes { get; init; } = [];
    }

    public class TopupBoxSectionViewModel
    {
        public int BoxId { get; init; }
        public string BoxName { get; init; } = string.Empty;
        public string BoxType { get; init; } = string.Empty;
        public List<TopupCreateLineViewModel> Lines { get; init; } = [];
    }

    public class TopupCreateLineViewModel
    {
        public int DenominationId { get; init; }
        public string DenominationName { get; init; } = string.Empty;
        public int DenomValue { get; init; }
        public long CurrentBaseline { get; init; }
    }

    // ── Details ─────────────────────────────────────────────────────────────

    public class TopupDetailsViewModel
    {
        public int Id { get; init; }
        public string BouBranchName { get; init; } = string.Empty;
        public string? Justification { get; init; }
        public string CurrentStatusName { get; init; } = string.Empty;
        public int CurrentStatusId { get; init; }
        public string CreatedBy { get; init; } = string.Empty;
        public DateTime CreatedDate { get; init; }
        public long TotalAmount { get; init; }

        public List<TopupDetailsLineViewModel> Lines { get; init; } = [];
        public List<TopupStatusHistoryViewModel> StatusHistory { get; init; } = [];
        public List<WorkflowTransitionDto> AvailableTransitions { get; init; } = [];
    }

    public class TopupDetailsLineViewModel
    {
        public int DenominationId { get; init; }
        public string DenominationName { get; init; } = string.Empty;
        public int DenomValue { get; init; }
        public long CurrentBaseline { get; init; }
        public long IncrementAmount { get; init; }
        public long NewBaseline => CurrentBaseline + IncrementAmount;
        public string BoxName { get; init; } = string.Empty;
    }

    public class TopupStatusHistoryViewModel
    {
        public string OldStatusName { get; init; } = string.Empty;
        public string NewStatusName { get; init; } = string.Empty;
        public string ChangedBy { get; init; } = string.Empty;
        public DateTime ChangedAt { get; init; }
        public string? Remarks { get; init; }
    }
}
