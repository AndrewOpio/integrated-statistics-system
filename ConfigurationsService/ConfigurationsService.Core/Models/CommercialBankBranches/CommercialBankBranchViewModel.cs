using ConfigurationsService.Core.Models.CommercialBanks;
using ConfigurationsService.Core.Models.Districts;

namespace ConfigurationsService.Core.Models.CommercialBankBranches;

public class CommercialBankBranchViewModel
{
    public int Id { get; set; }
    public int CommercialBankId { get; set; }
    public CommercialBankViewModel? CommercialBank { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int DistrictId { get; set; }
    public DistrictViewModel? District { get; set; }
    public bool IsDeleted { get; set; }
}