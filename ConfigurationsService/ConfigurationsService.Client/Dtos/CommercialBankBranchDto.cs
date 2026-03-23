namespace ConfigurationsService.Client;

public class CommercialBankBranchDto
{
    public int Id { get; set; }
    public int CommercialBankId { get; set; }
    public CommercialBankDto CommercialBank { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int DistrictId { get; set; }
    public DistrictDto District { get; set; }
}