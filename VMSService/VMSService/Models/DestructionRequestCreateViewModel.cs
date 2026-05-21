using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VMSService.Models
{
    public class DestructionRequestCreateViewModel
    {
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public string ReferenceNumber { get; set; }
        public string PanelA { get; set; }
        public string PanelB { get; set; }
        public string VerificationOfficer { get; set; }
        public string OtherMembers { get; set; }

        public List<DestructionRequestDenominationViewModel> Denominations { get; set; } = new();
        public List<DestructionRequestWitnessViewModel> Witnesses { get; set; } = new();
    }

    public class DestructionRequestDenominationViewModel
    {
        public int DenominationId { get; set; }
        public string NumberOfBags { get; set; }
        public string Amount { get; set; }
    }

    public class DestructionRequestWitnessViewModel
    {
        public string IDNumber { get; set; }
        public string FullName { get; set; }
    }
}
