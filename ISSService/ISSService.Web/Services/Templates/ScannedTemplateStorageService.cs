using System.Security.Cryptography;
using ScanService.Client;
using ScanService.Client.Dtos;

namespace ISSService.Web.Services.Templates;

public sealed class ScannedTemplateStorageService(
    IScanServiceClient scanServiceClient,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<ScannedTemplateStorageService> logger)
    : IScannedTemplateStorageService
{
    private const string DefaultStorageFolder = "ISSFiles";
    private const string TemplateFolder = "templates";

    public async Task<ScannedTemplateStorageResult> ScanAndSaveAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return ScannedTemplateStorageResult.Failed(
                "Please select a non-empty template file.");
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
                "Could not send ISS template {FileName} to the Scan Service.",
                file.FileName);

            return ScannedTemplateStorageResult.Failed(
                "The template could not be scanned for malware and was not saved.");
        }

        if (!scanResult.IsClean || scanResult.Content is null)
        {
            logger.LogWarning(
                "ISS template {FileName} was rejected by the Scan Service. " +
                "Verdict: {Verdict}. Message: {Message}. Correlation: {CorrelationId}",
                file.FileName,
                scanResult.Verdict,
                scanResult.Message,
                scanResult.CorrelationId);

            var message = string.IsNullOrWhiteSpace(scanResult.Message)
                ? "The template did not pass the malware scan and was not saved."
                : scanResult.Message;

            return ScannedTemplateStorageResult.Failed(message);
        }

        var cleanContent = scanResult.Content;

        // Calculate the integrity hash from the exact bytes cleared
        // by the Bank's Scan Service.
        var fileHash = Convert.ToHexString(
            SHA256.HashData(cleanContent));

        var storedFileName = SafeFileName.Create(file.FileName);

        var storageRoot = ResolveStorageRoot();

        var templateStorageDirectory = Path.Combine(
            storageRoot,
            TemplateFolder);

        Directory.CreateDirectory(templateStorageDirectory);

        var fullPath = Path.Combine(
            templateStorageDirectory,
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
                "Clean ISS template {FileName} could not be written to {Path}.",
                file.FileName,
                fullPath);

            return ScannedTemplateStorageResult.Failed(
                "The template passed the malware scan but could not be saved.");
        }

        logger.LogInformation(
            "ISS template {OriginalFileName} stored as {StoredFileName}. " +
            "SHA-256: {FileHash}",
            file.FileName,
            storedFileName,
            fileHash);

        return new ScannedTemplateStorageResult
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
                    "Deleted ISS stored template {Path}.",
                    filePath);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Could not delete ISS stored template {Path}.",
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
                "Could not clean up ISS template file {Path}.",
                path);
        }
    }
}