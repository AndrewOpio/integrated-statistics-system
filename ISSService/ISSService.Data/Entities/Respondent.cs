namespace ISSService.Data.Entities;

public class Respondent
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public RespondentType RespondentType { get; set; }

    public string? Region { get; set; }

    public string? District { get; set; }

    public string? Address { get; set; }

    public string? PrimaryContactName { get; set; }

    public string? PrimaryContactEmail { get; set; }

    public string? PrimaryContactPhone { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public ICollection<RespondentCase> RespondentCases { get; set; }
        = new List<RespondentCase>();
}

public enum RespondentType
{
    SACCO = 1,
    InsuranceCompany = 2,
    ReinsuranceCompany = 3,
    BusinessEnterprise = 4,
    OtherInstitution = 5
}