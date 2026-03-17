namespace ConfigurationsService.Core.Models.Districts;

public class DistrictViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}