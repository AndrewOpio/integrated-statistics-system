using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ScanService.Api.Infrastructure;
using ScanService.Api.Options;
using ScanService.Api.Scanning;
using ScanService.Api.Storage;
using ScanService.Client;
using ScanService.Client.Dtos;

namespace ScanService.Api.Controllers
{
    /// <summary>
    /// Scans uploads on behalf of the other Aswam microservices.
    ///
    /// The flow is: the calling service streams the upload straight here, the bytes are held in
    /// memory only, AMSI hands them to the registered antimalware engine (Microsoft Defender), and
    /// the file reaches disk only if the engine clears it. A malicious file is never written.
    /// </summary>
    [ApiController]
    [Route("api/scan")]
    public class ScanController(
        IFileScanner _scanner,
        IScannedFileStore _fileStore,
        MultipartUploadReader _uploadReader,
        IOptions<ScanServiceOptions> _options,
        ILogger<ScanController> _logger) : ControllerBase
    {
        private readonly ScanServiceOptions _settings = _options.Value;

        /// <summary>
        /// Scans a file and returns the verdict. Nothing is stored - the caller decides what to do
        /// with a cleared buffer.
        /// </summary>
        [HttpPost]
        [DisableFormValueModelBinding]
        [ProducesResponseType(typeof(FileScanResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FileScanResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(FileScanResultDto), StatusCodes.Status413PayloadTooLarge)]
        [ProducesResponseType(typeof(FileScanResultDto), StatusCodes.Status503ServiceUnavailable)]
        public Task<IActionResult> Scan(CancellationToken cancellationToken) =>
            HandleAsync(ScanMode.VerdictOnly, cancellationToken);

        /// <summary>
        /// Scans a file and, if it is clean, returns the cleared bytes as the response body
        /// (application/octet-stream) with the verdict in X-Scan-* headers. A non-clean verdict or
        /// any failure comes back as JSON instead.
        /// </summary>
        /// <remarks>
        /// The endpoint for callers on other hosts: the scanner needs no access to their storage,
        /// they write the bytes we hand back. Nothing is stored here either way.
        /// </remarks>
        [HttpPost("content")]
        [DisableFormValueModelBinding]
        [Produces("application/octet-stream", "application/json")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FileScanResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(FileScanResultDto), StatusCodes.Status413PayloadTooLarge)]
        [ProducesResponseType(typeof(FileScanResultDto), StatusCodes.Status503ServiceUnavailable)]
        public Task<IActionResult> ScanAndReturnContent(CancellationToken cancellationToken) =>
            HandleAsync(ScanMode.ReturnContent, cancellationToken);

        /// <summary>
        /// Scans a file and, only if it is clean, writes it to the destination directory named by
        /// the calling microservice.
        /// </summary>
        /// <remarks>
        /// Only meaningful when this service can reach that destination - the same host, or a share
        /// both hosts see. Callers elsewhere should use the content endpoint instead.
        /// </remarks>
        [HttpPost("store")]
        [DisableFormValueModelBinding]
        [ProducesResponseType(typeof(FileScanResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(FileScanResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(FileScanResultDto), StatusCodes.Status413PayloadTooLarge)]
        [ProducesResponseType(typeof(FileScanResultDto), StatusCodes.Status503ServiceUnavailable)]
        public Task<IActionResult> ScanAndStore(CancellationToken cancellationToken) =>
            HandleAsync(ScanMode.Store, cancellationToken);

        /// <summary>Reports whether AMSI is usable on this host.</summary>
        [HttpGet("health")]
        [ProducesResponseType(typeof(ScannerHealthDto), StatusCodes.Status200OK)]
        public IActionResult Health() => Ok(new ScannerHealthDto
        {
            IsAvailable = _scanner.IsAvailable,
            Provider = _scanner.ProviderName,
            Message = _scanner.StatusMessage,
            FailClosed = _settings.FailClosed,
            MaxFileSizeBytes = _settings.MaxFileSizeBytes
        });

        /// <summary>What the caller wants done with a file once it has been cleared.</summary>
        private enum ScanMode
        {
            /// <summary>Report the verdict and nothing else.</summary>
            VerdictOnly,

            /// <summary>Hand the cleared bytes back for the caller to store.</summary>
            ReturnContent,

            /// <summary>Write the cleared bytes to a destination the caller named.</summary>
            Store
        }

        private async Task<IActionResult> HandleAsync(ScanMode mode, CancellationToken cancellationToken)
        {
            var correlationId = HttpContext.TraceIdentifier;

            // Kestrel's default 30 MB body cap would reject large uploads before we see them, so
            // line it up with the configured file size limit (plus room for the multipart envelope).
            var bodySizeFeature = HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (bodySizeFeature is { IsReadOnly: false })
            {
                bodySizeFeature.MaxRequestBodySize = _settings.MaxFileSizeBytes + 1024 * 1024;
            }

            UploadReadResult upload;
            try
            {
                upload = await _uploadReader.ReadAsync(Request, cancellationToken);
            }
            catch (InvalidDataException ex)
            {
                _logger.LogWarning(ex, "Malformed multipart upload. Correlation {CorrelationId}.", correlationId);
                return BadRequest(Reject(string.Empty, ScanVerdict.Rejected, ex.Message, correlationId));
            }
            catch (BadHttpRequestException ex)
            {
                _logger.LogWarning(ex, "Upload exceeded the request body limit. Correlation {CorrelationId}.", correlationId);
                return StatusCode(
                    StatusCodes.Status413PayloadTooLarge,
                    Reject(string.Empty, ScanVerdict.Rejected,
                        $"The upload is larger than the {_settings.MaxFileSizeBytes / (1024 * 1024)} MB limit.",
                        correlationId));
            }

            if (!upload.Success)
            {
                var status = upload.ExceededSizeLimit
                    ? StatusCodes.Status413PayloadTooLarge
                    : StatusCodes.Status400BadRequest;

                return StatusCode(status,
                    Reject(upload.FileName, ScanVerdict.Rejected, upload.ErrorMessage, correlationId));
            }

            var buffer = upload.Buffer!;
            var length = upload.Length;

            if (length == 0)
            {
                return BadRequest(Reject(upload.FileName, ScanVerdict.Rejected, "The file is empty.", correlationId));
            }

            // Cheap checks first - no point handing an .exe to the engine when we would refuse it
            // whatever the verdict.
            var extensionError = ValidateExtension(upload.FileName);
            if (extensionError is not null)
            {
                _logger.LogWarning(
                    "Rejected {FileName} on extension policy. Correlation {CorrelationId}.",
                    upload.FileName, correlationId);

                return BadRequest(Reject(upload.FileName, ScanVerdict.Rejected, extensionError, correlationId, length));
            }

            // Step 4-6 of the flow: the buffer goes to AMSI, AMSI to the antimalware engine.
            var outcome = await _scanner.ScanAsync(buffer, length, upload.FileName, cancellationToken);

            if (outcome.Verdict == ScanVerdict.ScannerUnavailable || outcome.Verdict == ScanVerdict.Error)
            {
                if (_settings.FailClosed)
                {
                    _logger.LogError(
                        "Refusing {FileName} because it could not be scanned: {Message}. Correlation {CorrelationId}.",
                        upload.FileName, outcome.Message, correlationId);

                    return StatusCode(
                        StatusCodes.Status503ServiceUnavailable,
                        Reject(upload.FileName, outcome.Verdict,
                            "The file could not be scanned for malware, so it was not saved.",
                            correlationId, length));
                }

                _logger.LogWarning(
                    "FailClosed is off: letting through unscanned file {FileName}. Correlation {CorrelationId}.",
                    upload.FileName, correlationId);
            }
            else if (!outcome.IsClean)
            {
                // Step 8: malicious content is rejected here and never reaches the file store.
                _logger.LogWarning(
                    "Rejected {FileName}: {Message} AMSI_RESULT={Result}. Correlation {CorrelationId}.",
                    upload.FileName, outcome.Message, outcome.AmsiResultCode, correlationId);

                return Ok(Reject(upload.FileName, outcome.Verdict, outcome.Message, correlationId, length,
                    outcome.AmsiResultCode));
            }

            var result = new FileScanResultDto
            {
                IsClean = true,
                Verdict = ScanVerdict.Clean,
                FileName = upload.FileName,
                FileSizeBytes = length,
                Message = outcome.Message,
                AmsiResultCode = outcome.AmsiResultCode,
                ScannedBy = $"{_scanner.ProviderName}@{Environment.MachineName}",
                ScannedAtUtc = DateTimeOffset.UtcNow,
                CorrelationId = correlationId
            };

            if (mode == ScanMode.VerdictOnly)
            {
                return Ok(result);
            }

            if (mode == ScanMode.ReturnContent)
            {
                // Hand the exact buffer the engine cleared back to the caller. The verdict rides in
                // headers so the body can stay raw bytes instead of base64 inside JSON.
                Response.Headers[ScanHeaders.Verdict] = nameof(ScanVerdict.Clean);
                Response.Headers[ScanHeaders.AmsiResult] = outcome.AmsiResultCode.ToString();
                Response.Headers[ScanHeaders.CorrelationId] = correlationId;
                Response.Headers[ScanHeaders.Provider] = result.ScannedBy;
                Response.Headers[ScanHeaders.ScannedAt] = result.ScannedAtUtc.ToString("O");

                // MemoryStream over the existing array: no second copy of the file.
                return File(
                    new MemoryStream(buffer, 0, length, writable: false),
                    "application/octet-stream",
                    string.IsNullOrWhiteSpace(upload.FileName) ? "upload.bin" : upload.FileName);
            }

            // Step 7: cleared, so write it where the requesting microservice asked for it.
            var destination = new StoreFileRequestDto
            {
                DestinationDirectory = upload.GetField("destinationDirectory") ?? string.Empty,
                DestinationFileName = upload.GetField("destinationFileName"),
                Overwrite = bool.TryParse(upload.GetField("overwrite"), out var overwrite) && overwrite
            };

            if (string.IsNullOrWhiteSpace(destination.DestinationDirectory))
            {
                return BadRequest(Reject(upload.FileName, ScanVerdict.Rejected,
                    "No destination directory was supplied.", correlationId, length));
            }

            var stored = await _fileStore.StoreAsync(
                buffer, length, upload.FileName, destination, cancellationToken);

            if (!stored.Success)
            {
                // A destination we will not write to is the caller's mistake; a failed write is ours.
                return StatusCode(
                    stored.IsCallerError
                        ? StatusCodes.Status400BadRequest
                        : StatusCodes.Status500InternalServerError,
                    Reject(upload.FileName,
                        stored.IsCallerError ? ScanVerdict.Rejected : ScanVerdict.Error,
                        stored.Message, correlationId, length, outcome.AmsiResultCode));
            }

            result.StoredPath = stored.FullPath;
            result.StoredDirectory = stored.Directory;
            result.StoredFileName = stored.FileName;
            result.Message = stored.Message;

            return Ok(result);
        }

        /// <summary>
        /// Extension policy applied before scanning. Blocked extensions always lose; when an allow
        /// list is configured, anything outside it loses too.
        /// </summary>
        private string? ValidateExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName);

            if (string.IsNullOrWhiteSpace(extension))
            {
                return _settings.AllowedExtensions.Count > 0
                    ? "Files without an extension are not accepted."
                    : null;
            }

            if (_settings.BlockedExtensions.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase)))
            {
                return $"{extension} files are not accepted.";
            }

            if (_settings.AllowedExtensions.Count > 0 &&
                !_settings.AllowedExtensions.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase)))
            {
                return $"{extension} files are not accepted.";
            }

            return null;
        }

        private static FileScanResultDto Reject(
            string fileName,
            ScanVerdict verdict,
            string message,
            string correlationId,
            long sizeBytes = 0,
            int amsiResultCode = -1) =>
            new()
            {
                IsClean = false,
                Verdict = verdict,
                FileName = fileName,
                FileSizeBytes = sizeBytes,
                Message = message,
                AmsiResultCode = amsiResultCode,
                ScannedAtUtc = DateTimeOffset.UtcNow,
                CorrelationId = correlationId
            };
    }
}
