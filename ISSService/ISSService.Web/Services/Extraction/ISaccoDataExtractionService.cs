namespace ISSService.Web.Services.Extraction;

public interface ISaccoDataExtractionService
{
    Task<SaccoDataExtractionResult> ExtractAsync(
        int submissionId,
        string extractedBy,
        CancellationToken cancellationToken = default);
}

public sealed class SaccoDataExtractionResult
{
    public bool Success { get; init; }

    public int? OperationalDatasetId { get; init; }

    public int ObservationCount { get; init; }

    public string Message { get; init; } = string.Empty;

    public static SaccoDataExtractionResult Failed(
        string message)
    {
        return new SaccoDataExtractionResult
        {
            Success = false,
            Message = message
        };
    }

    public static SaccoDataExtractionResult Completed(
        int operationalDatasetId,
        int observationCount,
        string message)
    {
        return new SaccoDataExtractionResult
        {
            Success = true,
            OperationalDatasetId = operationalDatasetId,
            ObservationCount = observationCount,
            Message = message
        };
    }
}