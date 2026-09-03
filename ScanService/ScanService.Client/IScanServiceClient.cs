using ScanService.Client.Dtos;

namespace ScanService.Client
{
    /// <summary>
    /// Talks to the Aswam Scan Service. Every method streams the upload straight through to the
    /// scanner - the calling microservice never writes the bytes to disk itself.
    /// </summary>
    public interface IScanServiceClient
    {
        /// <summary>
        /// Scans a buffer and returns the verdict without storing anything anywhere.
        /// Use when the caller wants to decide for itself what to do with a cleared file.
        /// </summary>
        Task<FileScanResultDto> ScanAsync(
            Stream content,
            string fileName,
            string? contentType = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Scans a buffer and, only if the engine clears it, hands the cleared bytes back on
        /// <see cref="FileScanResultDto.Content"/> for the caller to store wherever it likes.
        /// Nothing is returned for a malicious file.
        /// </summary>
        /// <remarks>
        /// This is the option to use when the scan service runs on a different host from the
        /// caller: the scanner needs no access to the caller's storage, which is what lets one
        /// deployment serve every microservice.
        /// </remarks>
        Task<FileScanResultDto> ScanAndGetContentAsync(
            Stream content,
            string fileName,
            string? contentType = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Scans a buffer and, only if the engine clears it, writes it to the destination the
        /// caller specified. A malicious file is never written to disk.
        /// </summary>
        /// <remarks>
        /// Only usable when the scan service can reach that destination itself - the same host, or
        /// a share both hosts see. Otherwise the file is written on the scanner's disk and the
        /// caller will not find it; prefer <see cref="ScanAndGetContentAsync"/> there.
        /// </remarks>
        Task<FileScanResultDto> ScanAndStoreAsync(
            Stream content,
            string fileName,
            StoreFileRequestDto destination,
            string? contentType = null,
            CancellationToken cancellationToken = default);

        /// <summary>Reports whether AMSI is wired up on the scan service host.</summary>
        Task<ScannerHealthDto> GetHealthAsync(CancellationToken cancellationToken = default);
    }
}
