namespace ISSService.Data.Entities;

public class CycleTemplateAssignment
{
    public int Id { get; set; }

    public int ReportingCycleId { get; set; }

    public int DataCollectionTemplateVersionId { get; set; }

    public bool IsPrimary { get; set; } = true;

    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    public string? AssignedBy { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public ReportingCycle ReportingCycle { get; set; } = null!;

    public DataCollectionTemplateVersion DataCollectionTemplateVersion { get; set; } = null!;
}