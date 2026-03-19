using ConfigurationsService.Entities.AuditColumns;

namespace ConfigurationsService.Core.Models.CommercialBanks;

public class CommercialBankDto : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}