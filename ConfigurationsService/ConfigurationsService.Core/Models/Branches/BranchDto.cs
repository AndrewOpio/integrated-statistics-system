using ConfigurationsService.Entities.AuditColumns;

namespace ConfigurationsService.Core.Models.Branches;

public class BranchDto : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}