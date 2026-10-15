using Microsoft.AspNetCore.Http;

namespace ISSService.Web.Services.Submissions;

public interface IScannedSubmissionStorageService
{
    Task<ScannedSubmissionStorageResult> ScanAndSaveAsync(
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string? filePath,
        CancellationToken cancellationToken = default);
}

public sealed class ScannedSubmissionStorageResult
{
    public bool Success { get; init; }

    public string? Error { get; init; }

    public string? OriginalFileName { get; init; }

    public string? StoredFileName { get; init; }

    public string? FilePath { get; init; }

    public string? FileHash { get; init; }

    public long FileSize { get; init; }

    public static ScannedSubmissionStorageResult Failed(string error)
    {
        return new ScannedSubmissionStorageResult
        {
            Success = false,
            Error = error
        };
    }
}