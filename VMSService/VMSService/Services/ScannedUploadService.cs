using ScanService.Client;
using ScanService.Client.Dtos;
using VMSService.Core.Static;

namespace VMSService.Web.Services
{
    /// <inheritdoc />
    public sealed class ScannedUploadService(
        IScanServiceClient _scanClient,
        ILogger<ScannedUploadService> _logger) : IScannedUploadService
    {
        public async Task<ScannedUploadBatchResult> ScanAndSaveAsync(
            IEnumerable<ScannedUploadRequest> uploads,
            CancellationToken cancellationToken = default)
        {
            var savedPaths = new Dictionary<string, string>(StringComparer.Ordinal);
            var savedAbsolutePaths = new List<string>();
            var errors = new List<string>();

            foreach (var upload in uploads)
            {
                if (upload.File is null || upload.File.Length == 0)
                {
                    continue;
                }

                FileScanResultDto result;
                try
                {
                    // OpenReadStream feeds the scan service directly - VMS never writes these bytes
                    // anywhere itself. What comes back is the buffer the engine actually cleared.
                    await using var stream = upload.File.OpenReadStream();

                    result = await _scanClient.ScanAndGetContentAsync(
                        stream,
                        upload.File.FileName,
                        upload.File.ContentType,
                        cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Could not send {FileName} to the scan service.", upload.File.FileName);
                    result = FileScanResultDto.Failed(
                        upload.File.FileName,
                        ScanVerdict.ScannerUnavailable,
                        "The file could not be scanned for malware, so it was not saved.");
                }

                if (result.IsClean && result.Content is not null)
                {
                    try
                    {
                        var saved = await WriteAsync(
                            result.Content, upload.File.FileName, upload.FolderName, cancellationToken);

                        savedPaths[upload.FieldName] = $"/uploads/{upload.FolderName}/{saved.FileName}";
                        savedAbsolutePaths.Add(saved.FullPath);
                        continue;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Scanned file {FileName} could not be saved.", upload.File.FileName);
                        errors.Add($"{upload.File.FileName}: the file was clean but could not be saved. Please try again.");
                        RollBack(savedAbsolutePaths);
                        return new ScannedUploadBatchResult { Success = false, Errors = errors };
                    }
                }

                _logger.LogWarning(
                    "Upload {FileName} rejected with verdict {Verdict}: {Message} (correlation {CorrelationId})",
                    upload.File.FileName, result.Verdict, result.Message, result.CorrelationId);

                errors.Add($"{upload.File.FileName}: {result.Message}");

                // Nothing partial: drop whatever earlier files in this batch were already stored.
                RollBack(savedAbsolutePaths);

                return new ScannedUploadBatchResult { Success = false, Errors = errors };
            }

            return new ScannedUploadBatchResult { Success = true, SavedPaths = savedPaths };
        }

        /// <summary>
        /// Writes cleared bytes into the uploads folder. This is the only place VMS puts a user
        /// supplied file on disk, and it is only ever reached with bytes the scanner returned.
        /// </summary>
        private static async Task<(string FullPath, string FileName)> WriteAsync(
            byte[] content, string originalFileName, string folderName, CancellationToken cancellationToken)
        {
            var folder = Path.Combine(FileStorageConstants.UploadsRoot, folderName);
            Directory.CreateDirectory(folder);

            // Same "{guid}_{original}" shape the controllers produced before, from the shared helper.
            var fileName = SafeFileName.Create(originalFileName);
            var fullPath = Path.Combine(folder, fileName);

            await using (var stream = new FileStream(
                fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 81920, useAsync: true))
            {
                await stream.WriteAsync(content, cancellationToken);
            }

            return (fullPath, fileName);
        }

        private void RollBack(IEnumerable<string> absolutePaths)
        {
            foreach (var path in absolutePaths)
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
                    // Worth knowing about, but not worth failing the request over - the file is
                    // clean, it is just no longer referenced by anything.
                    _logger.LogError(ex, "Could not roll back stored upload {Path}.", path);
                }
            }
        }
    }
}
