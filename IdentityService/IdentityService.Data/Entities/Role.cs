using IdentityService.Data.AuditColumns;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace IdentityService.Data.Entities
{
    public class Role : IdentityRole, IAuditableEntity
    {
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        [Display(Name = "Creator")]
        public string CreatedBy { get; set; } = "system";

        [Display(Name = "Modifier")]
        public string ModifiedBy { get; set; } = "system";
    }
}
