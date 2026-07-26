namespace VMSService.Core.Models.Cfb
{
    /// <summary>Describes one denomination column in the supervisor overview grid.</summary>
    public class CfbDenomColumnDef
    {
        public int DenominationId { get; init; }
        public string Name { get; init; } = string.Empty;
        public int DenomValue { get; init; }
    }
}
