using ConfigurationsService.Core.Models.CommercialBanks;
using ConfigurationsService.Core.Models.Districts;
using ConfigurationsService.Entities.AuditColumns;

namespace ConfigurationsService.Core.Models.CommercialBankBranches;

public class CommercialBankBranchDto : AuditableEntity
{
    public int Id { get; set; }
    public int CommercialBankId { get; set; }
    public CommercialBankDto CommercialBank { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int DistrictId { get; set; }
    public DistrictDto District { get; set; }
    public bool IsDeleted { get; set; }
}