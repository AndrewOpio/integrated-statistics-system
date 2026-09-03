using Microsoft.Extensions.Options;
using ScanService.Api.Options;
using ScanService.Client;
using ScanService.Client.Dtos;

namespace ScanService.Api.Storage
{
    /// <inheritdoc />
    public sealed class ScannedFileStore(
        IOptions<ScanServiceOptions> _options,
        ILogger<ScannedFileStore> _logger) : IScannedFileStore
    {
        private readonly ScanServiceOptions _settings = _options.Value;

        public async Task<StoreResult> StoreAsync(
            byte[] buffer,
            int length,
            string originalFileName,
            StoreFileRequestDto destination,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(destination.DestinationDirectory))
            {
                return StoreResult.Rejected("No destination directory was supplied.");
            }

            string directory;
            try
            {
                directory = Path.GetFullPath(destination.DestinationDirectory);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                _logger.LogWarning(ex, "Rejected malformed destination {Destination}.", destination.DestinationDirectory);
                return StoreResult.Rejected("The destination directory is not a valid path.");
            }

            if (!IsUnderAllowedRoot(directory))
            {
                _logger.LogWarning(
                    "Rejected destination {Destination}: outside the configured allowed roots.", directory);
                return StoreResult.Rejected(
                    "The destination directory is outside the area this service is allowed to write to.");
            }

            var fileName = BuildFileName(originalFileName, destination.DestinationFileName);
            var fullPath = Path.GetFullPath(Path.Combine(directory, fileName));

            // Re-check after combining: a crafted file name containing "..\" could otherwise walk out
            // of the directory we just approved.
            if (!IsUnderAllowedRoot(fullPath) || !fullPath.StartsWith(EnsureTrailingSeparator(directory), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Rejected destination file name {FileName}: escapes the destination directory.", fileName);
                return StoreResult.Rejected("The destination file name is not valid.");
            }

            try
            {
                Directory.CreateDirectory(directory);

                var mode = destination.Overwrite ? FileMode.Create : FileMode.CreateNew;
                await using var stream = new FileStream(
                    fullPath, mode, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);

                await stream.WriteAsync(buffer.AsMemory(0, length), cancellationToken);
                await stream.FlushAsync(cancellationToken);

                _logger.LogInformation(
                    "Stored cleared file {FileName} ({Bytes} bytes) at {FullPath}.", fileName, length, fullPath);

                return new StoreResult(true, fullPath, directory, fileName, "The file was scanned and saved.");
            }
            catch (IOException ex) when (!destination.Overwrite && File.Exists(fullPath))
            {
                _logger.LogWarning(ex, "A file already exists at {FullPath}.", fullPath);
                return StoreResult.Rejected("A file with that name already exists at the destination.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write cleared file to {FullPath}.", fullPath);

                // Never leave a half written file behind.
                TryDelete(fullPath);
                return StoreResult.Failed("The file was clean but could not be saved to its destination.");
            }
        }

        private bool IsUnderAllowedRoot(string fullPath)
        {
            if (_settings.AllowedDestinationRoots.Count == 0)
            {
                // No roots configured means no writes are permitted - refusing is safer than
                // letting a caller name any directory on the host.
                _logger.LogError(
                    "ScanService:AllowedDestinationRoots is empty, so no destination can be accepted.");
                return false;
            }

            foreach (var root in _settings.AllowedDestinationRoots)
            {
                if (string.IsNullOrWhiteSpace(root))
                {
                    continue;
                }

                var fullRoot = EnsureTrailingSeparator(Path.GetFullPath(root));
                if (EnsureTrailingSeparator(fullPath).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Produces the on-disk name: the caller's own choice if it gave one, otherwise
        /// "{guid}_{original}". Shared with the client library so callers that store files
        /// themselves name them identically.
        /// </summary>
        private static string BuildFileName(string originalFileName, string? requestedFileName) =>
            string.IsNullOrWhiteSpace(requestedFileName)
                ? SafeFileName.Create(originalFileName)
                : SafeFileName.Sanitise(requestedFileName) ?? SafeFileName.Create(originalFileName);

        private static string EnsureTrailingSeparator(string path) =>
            path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;

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
                _logger.LogError(ex, "Could not clean up partially written file {FullPath}.", path);
            }
        }
    }
}
