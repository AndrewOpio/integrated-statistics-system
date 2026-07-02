using System.ComponentModel.DataAnnotations;

namespace VMSService.Core.Models.DestructionTypes
{
    public class DestructionTypeViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }
    }
}
