namespace IdentityService.Core.Models
{
    public class ManageBranchesViewModel
    {
        public int ApplicationRoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public List<BranchSelectViewModel> Branches { get; set; } = [];
    }

    public class BranchSelectViewModel
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string BranchCode { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
