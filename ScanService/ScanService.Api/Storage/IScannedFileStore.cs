using ScanService.Client.Dtos;

namespace ScanService.Api.Storage
{
    /// <summary>
    /// Writes files that the scanner has cleared. Nothing else in the service touches the file
    /// system, so a buffer can only reach disk after it has been through <c>AmsiScanBuffer</c>.
    /// </summary>
    public interface IScannedFileStore
    {
        Task<StoreResult> StoreAsync(
            byte[] buffer,
            int length,
            string originalFileName,
            StoreFileRequestDto destination,
            CancellationToken cancellationToken);
    }

    public sealed record StoreResult(
        bool Success,
        string? FullPath,
        string? Directory,
        string? FileName,
        string Message,
        bool IsCallerError = false)
    {
        /// <summary>The destination the caller asked for is not one we will write to.</summary>
        public static StoreResult Rejected(string message) => new(false, null, null, null, message, true);

        /// <summary>The destination was fine but the write failed on our side.</summary>
        public static StoreResult Failed(string message) => new(false, null, null, null, message);
    }
}
