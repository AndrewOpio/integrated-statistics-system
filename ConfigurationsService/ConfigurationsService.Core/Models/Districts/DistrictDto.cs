using ConfigurationsService.Entities.AuditColumns;

namespace ConfigurationsService.Core.Models.Districts;

public class DistrictDto : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}