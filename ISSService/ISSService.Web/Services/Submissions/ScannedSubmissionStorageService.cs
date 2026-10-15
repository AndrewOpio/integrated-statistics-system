using System.Security.Cryptography;
using ScanService.Client;
using ScanService.Client.Dtos;

namespace ISSService.Web.Services.Submissions;

public sealed class ScannedSubmissionStorageService(
    IScanServiceClient scanServiceClient,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<ScannedSubmissionStorageService> logger)
    : IScannedSubmissionStorageService
{
    private const string DefaultStorageFolder = "ISSFiles";
    private const string SubmissionFolder = "submissions";

    public async Task<ScannedSubmissionStorageResult> ScanAndSaveAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return ScannedSubmissionStorageResult.Failed(
                "Please select a non-empty submission file.");
        }

        FileScanResultDto scanResult;

        try
        {
            await using var inputStream = file.OpenReadStream();

            scanResult = await scanServiceClient.ScanAndGetContentAsync(
                inputStream,
                file.FileName,
                file.ContentType,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Could not send ISS submission {FileName} to the Scan Service.",
                file.FileName);

            return ScannedSubmissionStorageResult.Failed(
                "The submission could not be scanned for malware and was not saved.");
        }

        if (!scanResult.IsClean || scanResult.Content is null)
        {
            logger.LogWarning(
                "ISS submission {FileName} was rejected by the Scan Service. " +
                "Verdict: {Verdict}. Message: {Message}. Correlation: {CorrelationId}",
                file.FileName,
                scanResult.Verdict,
                scanResult.Message,
                scanResult.CorrelationId);

            var message = string.IsNullOrWhiteSpace(scanResult.Message)
                ? "The submission did not pass the malware scan and was not saved."
                : scanResult.Message;

            return ScannedSubmissionStorageResult.Failed(message);
        }

        var cleanContent = scanResult.Content;

        // Calculate the integrity hash from the exact bytes cleared
        // by the Bank's Scan Service.
        var fileHash = Convert.ToHexString(
            SHA256.HashData(cleanContent));

        var storedFileName = SafeFileName.Create(file.FileName);

        var storageRoot = ResolveStorageRoot();

        var submissionStorageDirectory = Path.Combine(
            storageRoot,
            SubmissionFolder);

        Directory.CreateDirectory(submissionStorageDirectory);

        var fullPath = Path.Combine(
            submissionStorageDirectory,
            storedFileName);

        try
        {
            await using var outputStream = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await outputStream.WriteAsync(
                cleanContent,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryDelete(fullPath);
            throw;
        }
        catch (Exception ex)
        {
            TryDelete(fullPath);

            logger.LogError(
                ex,
                "Clean ISS submission {FileName} could not be written to {Path}.",
                file.FileName,
                fullPath);

            return ScannedSubmissionStorageResult.Failed(
                "The submission passed the malware scan but could not be saved.");
        }

        logger.LogInformation(
            "ISS submission {OriginalFileName} stored as {StoredFileName}. " +
            "SHA-256: {FileHash}",
            file.FileName,
            storedFileName,
            fileHash);

        return new ScannedSubmissionStorageResult
        {
            Success = true,
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            FilePath = fullPath,
            FileHash = fileHash,
            FileSize = cleanContent.LongLength
        };
    }

    public Task DeleteAsync(
        string? filePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Task.CompletedTask;
        }

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);

                logger.LogInformation(
                    "Deleted ISS stored submission {Path}.",
                    filePath);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Could not delete ISS stored submission {Path}.",
                filePath);

            throw;
        }

        return Task.CompletedTask;
    }

    private string ResolveStorageRoot()
    {
        var configuredRoot =
            configuration["FileStorage:UploadsRoot"];

        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            return Path.GetFullPath(configuredRoot);
        }

        // Development fallback only. Production should provide
        // FileStorage:UploadsRoot through environment configuration.
        return Path.Combine(
            environment.ContentRootPath,
            DefaultStorageFolder);
    }

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Could not clean up ISS submission file {Path}.",
                path);
        }
    }
}