namespace ISSService.Data.Entities;

public class DataCollectionTemplate
{
    public int Id { get; set; }

    public int StatisticalProcessId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DataCollectionTemplateType TemplateType { get; set; }

    public bool IsRespondentSpecific { get; set; }

    public bool SupportsPreviousPeriodData { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public StatisticalProcess StatisticalProcess { get; set; } = null!;

    public ICollection<DataCollectionTemplateVersion> Versions { get; set; }
        = new List<DataCollectionTemplateVersion>();
}

public enum DataCollectionTemplateType
{
    ExcelTemplate = 1,
    ElectronicQuestionnaire = 2,
    FieldCollectionForm = 3,
    AdministrativeDataTemplate = 4,
    Hybrid = 5
}