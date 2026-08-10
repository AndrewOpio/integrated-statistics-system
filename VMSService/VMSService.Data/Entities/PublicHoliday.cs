using System.ComponentModel.DataAnnotations;
using VMSService.Data.AuditColumns;

namespace VMSService.Data.Entities
{
    public class PublicHoliday : AuditableEntity
    {
        public int Id { get; set; }

        public DateOnly Date { get; set; }

        [MaxLength(200)]
        public string Name { get; set; } = null!;

        public bool Active { get; set; } = true;
    }
}
