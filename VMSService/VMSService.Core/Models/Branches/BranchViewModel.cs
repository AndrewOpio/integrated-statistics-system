namespace VMSService.Core.Models.Branches
{
    public class BranchViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}