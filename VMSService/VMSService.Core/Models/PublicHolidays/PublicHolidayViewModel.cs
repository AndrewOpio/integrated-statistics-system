using System.ComponentModel.DataAnnotations;

namespace VMSService.Core.Models.PublicHolidays
{
    public class PublicHolidayViewModel
    {
        public int Id { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateOnly Date { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public bool Active { get; set; } = true;

        /// <summary>True once Date has passed — Edit is locked, but Activate/Deactivate remains open.</summary>
        public bool IsPast { get; set; }
    }
}
