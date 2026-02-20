using Microsoft.AspNetCore.Identity;

namespace GatewayService.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string Title { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? DisableDate { get; set; }
        public DateTime? EndDate { get; set; }
        //public ICollection<UserCostCenter> UserCostCenters { get; set; }
        public bool IsLoggedIn { get; set; }
        public DateTime LastActivity { get; set; }
    }
}
