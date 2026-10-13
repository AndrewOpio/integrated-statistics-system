namespace ISSService.Data.Entities;

public class RespondentCaseStatusHistory
{
    public int Id { get; set; }

    public int RespondentCaseId { get; set; }

    // WorkflowService transition that authorised this movement.
    public int? WorkflowTransitionId { get; set; }

    public int OldStatusId { get; set; }

    public string OldStatusName { get; set; } = string.Empty;

    public int NewStatusId { get; set; }

    public string NewStatusName { get; set; } = string.Empty;

    public string ActionName { get; set; } = string.Empty;

    public string? Remarks { get; set; }

    public DateTime ChangedDate { get; set; } = DateTime.UtcNow;

    public string ChangedBy { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public RespondentCase RespondentCase { get; set; } = null!;
}