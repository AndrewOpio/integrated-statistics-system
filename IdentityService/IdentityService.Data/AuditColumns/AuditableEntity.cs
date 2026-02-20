using System.ComponentModel.DataAnnotations;

namespace IdentityService.Data.AuditColumns;
public class AuditableEntity : IAuditableEntity
{
	public DateTime CreatedDate { get; set; } = DateTime.Now;
	public DateTime ModifiedDate { get; set; } = DateTime.Now;

	[Display(Name = "Creator")]
	public string CreatedBy { get; set; } = "system";

	[Display(Name = "Modifier")]
	public string ModifiedBy { get; set; } = "system";
}       
