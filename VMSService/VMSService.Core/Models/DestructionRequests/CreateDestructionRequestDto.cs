namespace VMSService.Core.Models.DestructionRequests
{
    public class UpdateDestructionRequestDto
    {
        public int Id { get; set; }
        public int? DestructionAuthorizationId { get; set; }
        public int DestructionTypeId { get; set; }
        public int? DestructionLocationId { get; set; }
        public int BranchId { get; set; }
        public string ReferenceNumber { get; set; } = null!;
        public DateTime Date { get; set; }
        public string VerificationOfficer { get; set; } = null!;
        public List<DestructionDenominationDto> Denominations { get; set; } = new();
        public List<DestructionMemberDto> Members { get; set; } = new();
    }

    public class CreateDestructionRequestDto
    {
        public int? DestructionAuthorizationId { get; set; }
        public int DestructionTypeId { get; set; }
        public int? DestructionLocationId { get; set; }
        public int BranchId { get; set; }
        public string ReferenceNumber { get; set; } = null!;
        public DateTime Date { get; set; }
        public string VerificationOfficer { get; set; } = null!;
        public List<DestructionDenominationDto> Denominations { get; set; } = new();
        public List<DestructionMemberDto> Members { get; set; } = new();
    }

    public class DestructionDenominationDto
    {
        public int DenominationId { get; set; }
        public string? NumberOfBags { get; set; }
        public string? NumberOfNotes { get; set; }
        public decimal Amount { get; set; }
    }

    public class DestructionMemberDto
    {
        public string IDNumber { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public bool IsWitness { get; set; }
    }

    public class RecordDestructionDto
    {
        public int DestructionRequestId { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public List<DenominationDestroyedDto> Denominations { get; set; } = new();
    }

    public class DenominationDestroyedDto
    {
        public int DenominationId { get; set; }
        public decimal DestroyedToday { get; set; }
    }
}
