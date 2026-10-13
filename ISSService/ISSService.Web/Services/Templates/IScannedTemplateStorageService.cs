using Microsoft.AspNetCore.Http;

namespace ISSService.Web.Services.Templates;

public interface IScannedTemplateStorageService
{
    Task<ScannedTemplateStorageResult> ScanAndSaveAsync(
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string? filePath,
        CancellationToken cancellationToken = default);
}

public sealed class ScannedTemplateStorageResult
{
    public bool Success { get; init; }

    public string? Error { get; init; }

    public string? OriginalFileName { get; init; }

    public string? StoredFileName { get; init; }

    public string? FilePath { get; init; }

    public string? FileHash { get; init; }

    public long FileSize { get; init; }

    public static ScannedTemplateStorageResult Failed(string error)
    {
        return new ScannedTemplateStorageResult
        {
            Success = false,
            Error = error
        };
    }
}