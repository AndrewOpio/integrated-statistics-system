namespace ISSService.Data.Entities;

public class DataCollectionTemplateVersion
{
    public int Id { get; set; }

    public int DataCollectionTemplateId { get; set; }

    public int VersionNumber { get; set; }

    public string VersionLabel { get; set; } = string.Empty;

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public TemplateVersionStatus Status { get; set; } = TemplateVersionStatus.Draft;

    public string? FileName { get; set; }

    public string? FilePath { get; set; }

    public string? FileHash { get; set; }

    public string? ChangeNotes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public string? ApprovedBy { get; set; }

    public DataCollectionTemplate DataCollectionTemplate { get; set; } = null!;

    public ICollection<CycleTemplateAssignment> CycleAssignments { get; set; }
        = new List<CycleTemplateAssignment>();
}

public enum TemplateVersionStatus
{
    Draft = 1,
    Approved = 2,
    Active = 3,
    Retired = 4
}