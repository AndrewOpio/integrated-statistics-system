using System.ComponentModel.DataAnnotations;

namespace GatewayService.Models
{
    public class Application
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Acronym { get; set; }
        public string Status { get; set; }
        public string? Url { get; set; }
        public DateTime CreatedDate { get; set; } 
        public DateTime ModifiedDate { get; set; }

        [Display(Name = "Creator")]
        public string CreatedBy { get; set; } 

        [Display(Name = "Modifier")]
        public string ModifiedBy { get; set; }
    }
}
