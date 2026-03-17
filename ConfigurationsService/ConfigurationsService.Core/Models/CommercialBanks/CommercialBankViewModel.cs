namespace ConfigurationsService.Core.Models.CommercialBanks;

public class CommercialBankViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SwiftCode { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}