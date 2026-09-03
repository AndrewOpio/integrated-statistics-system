using Microsoft.Extensions.Logging;
using ScanService.Client.Dtos;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ScanService.Client
{
    /// <inheritdoc />
    /// <remarks>
    /// Deliberately fails closed: any transport problem, timeout or unreadable response comes back
    /// as a non-clean <see cref="FileScanResultDto"/> rather than an exception, so a caller that
    /// only checks <see cref="FileScanResultDto.IsClean"/> can never accidentally save an unscanned
    /// file because the scanner was down.
    /// </remarks>
    public class ScanServiceClient(HttpClient _httpClient, ILogger<ScanServiceClient> _logger) : IScanServiceClient
    {
        public Task<FileScanResultDto> ScanAsync(
            Stream content,
            string fileName,
            string? contentType = null,
            CancellationToken cancellationToken = default) =>
            PostFileAsync("api/scan", content, fileName, contentType, destination: null,
                expectContent: false, cancellationToken);

        public Task<FileScanResultDto> ScanAndGetContentAsync(
            Stream content,
            string fileName,
            string? contentType = null,
            CancellationToken cancellationToken = default) =>
            PostFileAsync("api/scan/content", content, fileName, contentType, destination: null,
                expectContent: true, cancellationToken);

        public Task<FileScanResultDto> ScanAndStoreAsync(
            Stream content,
            string fileName,
            StoreFileRequestDto destination,
            string? contentType = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(destination);

            if (string.IsNullOrWhiteSpace(destination.DestinationDirectory))
            {
                throw new ArgumentException(
                    "A destination directory is required.", nameof(destination));
            }

            return PostFileAsync("api/scan/store", content, fileName, contentType, destination,
                expectContent: false, cancellationToken);
        }

        public async Task<ScannerHealthDto> GetHealthAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _httpClient.GetAsync("api/scan/health", cancellationToken);
                var health = await response.Content.ReadFromJsonSafeAsync<ScannerHealthDto>(cancellationToken);

                return health ?? new ScannerHealthDto
                {
                    IsAvailable = false,
                    Message = $"Scan service returned {(int)response.StatusCode} with no readable body."
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The caller gave up; that is not a health verdict.
                throw;
            }
            catch (Exception ex)
            {
                // Everything else - connection refused, and timeouts, which surface as
                // TaskCanceledException with no cancellation requested - means "not reachable".
                _logger.LogError(ex, "Could not reach the scan service health endpoint.");
                return new ScannerHealthDto
                {
                    IsAvailable = false,
                    Message = "The scan service could not be reached."
                };
            }
        }

        private async Task<FileScanResultDto> PostFileAsync(
            string endpoint,
            Stream content,
            string fileName,
            string? contentType,
            StoreFileRequestDto? destination,
            bool expectContent,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(content);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "upload.bin";
            }

            try
            {
                // StreamContent copies straight from the caller's stream onto the wire, so the
                // bytes are never materialised into a file on the calling service.
                using var form = new MultipartFormDataContent();
                var fileContent = new StreamContent(content);
                fileContent.Headers.ContentType =
                    MediaTypeHeaderValue.Parse(string.IsNullOrWhiteSpace(contentType)
                        ? "application/octet-stream"
                        : contentType);
                form.Add(fileContent, "file", fileName);

                if (destination is not null)
                {
                    form.Add(new StringContent(destination.DestinationDirectory), "destinationDirectory");
                    form.Add(new StringContent(destination.Overwrite.ToString()), "overwrite");

                    if (!string.IsNullOrWhiteSpace(destination.DestinationFileName))
                    {
                        form.Add(new StringContent(destination.DestinationFileName), "destinationFileName");
                    }
                }

                using var response = await _httpClient.PostAsync(endpoint, form, cancellationToken);

                // The content endpoint answers a clean file with the raw bytes and puts the verdict
                // in headers; everything else (and every rejection) comes back as the JSON DTO.
                if (expectContent && IsBinaryResponse(response))
                {
                    return await ReadClearedFileAsync(response, fileName, cancellationToken);
                }

                var result = await response.Content.ReadFromJsonSafeAsync<FileScanResultDto>(cancellationToken);

                if (result is not null)
                {
                    return result;
                }

                _logger.LogError(
                    "Scan service returned {StatusCode} with no readable body for {FileName}.",
                    (int)response.StatusCode, fileName);

                return FileScanResultDto.Failed(
                    fileName,
                    ScanVerdict.Error,
                    "The file could not be scanned and was not saved. Please try again.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Includes timeouts (TaskCanceledException) and connection failures.
                _logger.LogError(ex, "Scan request failed for {FileName}.", fileName);

                return FileScanResultDto.Failed(
                    fileName,
                    ScanVerdict.ScannerUnavailable,
                    "The virus scanning service could not be reached, so the file was not saved.");
            }
        }

        private static bool IsBinaryResponse(HttpResponseMessage response) =>
            response.IsSuccessStatusCode &&
            string.Equals(
                response.Content.Headers.ContentType?.MediaType,
                "application/octet-stream",
                StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Rebuilds the verdict from the response headers and reads the cleared bytes from the body.
        /// The verdict travels in headers rather than in the body so the file itself does not have
        /// to be base64 encoded into JSON.
        /// </summary>
        private static async Task<FileScanResultDto> ReadClearedFileAsync(
            HttpResponseMessage response, string fileName, CancellationToken cancellationToken)
        {
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            return new FileScanResultDto
            {
                IsClean = true,
                Verdict = ScanVerdict.Clean,
                FileName = fileName,
                FileSizeBytes = bytes.LongLength,
                Message = "No threats were detected.",
                AmsiResultCode = int.TryParse(Header(response, ScanHeaders.AmsiResult), out var code) ? code : -1,
                ScannedBy = Header(response, ScanHeaders.Provider),
                CorrelationId = Header(response, ScanHeaders.CorrelationId),
                ScannedAtUtc = DateTimeOffset.TryParse(Header(response, ScanHeaders.ScannedAt), out var at)
                    ? at
                    : DateTimeOffset.UtcNow,
                Content = bytes
            };
        }

        private static string? Header(HttpResponseMessage response, string name) =>
            response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    }

    internal static class HttpContentJsonExtensions
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Reads the DTO whatever the status code was - the scan service returns a body for
        /// rejections and for 503s too, and that body carries the reason we want to show the user.
        /// </summary>
        internal static async Task<T?> ReadFromJsonSafeAsync<T>(
            this HttpContent httpContent,
            CancellationToken cancellationToken) where T : class
        {
            try
            {
                var json = await httpContent.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<T>(json, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
