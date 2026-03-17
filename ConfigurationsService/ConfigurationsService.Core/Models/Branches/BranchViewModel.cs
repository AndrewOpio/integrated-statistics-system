namespace ConfigurationsService.Core.Models.Branches;

public class BranchViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}