using System.ComponentModel.DataAnnotations;

namespace VMSService.Core.Models.BoardSessionDifferentDenoms
{
    public class SaveDifferentDenomsViewModel
    {
        [Required]
        public int SessionId { get; set; }

        public List<DifferentDenomEntryViewModel> Entries { get; set; } = [];
    }

    public class DifferentDenomEntryViewModel
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a denomination.")]
        public int DenominationId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Number of pieces must be at least 1.")]
        public int NumberOfPieces { get; set; }
    }
}
