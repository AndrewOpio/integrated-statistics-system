namespace ISSService.Data.Entities;

public class StatisticalProcess
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public StatisticalProcessType ProcessType { get; set; }

    public CollectionMode CollectionMode { get; set; }

    public ReportingFrequency ReportingFrequency { get; set; }

    public string? ResponsibleUnit { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public ICollection<ReportingCycle> ReportingCycles { get; set; }
        = new List<ReportingCycle>();

    public ICollection<DataCollectionTemplate> DataCollectionTemplates { get; set; }
        = new List<DataCollectionTemplate>();
}

public enum StatisticalProcessType
{
    Survey = 1,
    InstitutionalReturn = 2,
    AdministrativeData = 3
}

public enum CollectionMode
{
    Electronic = 1,
    TemplateUpload = 2,
    FieldCollection = 3,
    AdministrativeImport = 4,
    Hybrid = 5
}

public enum ReportingFrequency
{
    Monthly = 1,
    Quarterly = 2,
    SemiAnnual = 3,
    Annual = 4,
    AdHoc = 5
}