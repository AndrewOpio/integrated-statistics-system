using System.ComponentModel.DataAnnotations;

namespace VMSService.Core.Models.DestructionLocations
{
    public class DestructionLocationViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }
    }
}
