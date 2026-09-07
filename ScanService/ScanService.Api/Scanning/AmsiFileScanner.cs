using Microsoft.Extensions.Options;
using ScanService.Api.Amsi;
using ScanService.Api.Options;
using ScanService.Client.Dtos;
using System.Runtime.InteropServices;

namespace ScanService.Api.Scanning
{
    /// <summary>
    /// Scans buffers through the Windows Antimalware Scan Interface, which routes the content to the
    /// registered antimalware provider (Microsoft Defender on a default Windows install).
    /// </summary>
    /// <remarks>
    /// The AMSI context is initialised once for the process and reused; a fresh session is opened per
    /// scan so the provider treats each upload as its own piece of content. Concurrency is capped
    /// because each in-flight scan pins a full copy of the file in memory and the provider itself
    /// serialises heavy work anyway.
    /// </remarks>
    public sealed class AmsiFileScanner : IFileScanner, IDisposable
    {
        private readonly ILogger<AmsiFileScanner> _logger;
        private readonly ScanServiceOptions _options;
        private readonly SemaphoreSlim _concurrencyGate;
        private readonly AmsiContextSafeHandle? _context;

        public string ProviderName { get; }
        public bool IsAvailable { get; }
        public string StatusMessage { get; }

        public AmsiFileScanner(IOptions<ScanServiceOptions> options, ILogger<AmsiFileScanner> logger)
        {
            _logger = logger;
            _options = options.Value;
            ProviderName = _options.AmsiApplicationName;
            _concurrencyGate = new SemaphoreSlim(Math.Max(1, _options.MaxConcurrentScans));

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IsAvailable = false;
                StatusMessage = "AMSI is a Windows API; this host is not running Windows.";
                _logger.LogError("Scan service started on a non-Windows host. {Status}", StatusMessage);
                return;
            }

            try
            {
                var hr = AmsiNativeMethods.AmsiInitialize(_options.AmsiApplicationName, out var context);
                if (hr != 0 || context is null || context.IsInvalid)
                {
                    context?.Dispose();
                    IsAvailable = false;
                    StatusMessage =
                        $"AmsiInitialize failed with HRESULT 0x{hr:X8}. Check that an antimalware " +
                        "provider (e.g. Microsoft Defender) is installed and running.";
                    _logger.LogError("{Status}", StatusMessage);
                    return;
                }

                _context = context;
                IsAvailable = true;
                StatusMessage = "AMSI initialised.";
                _logger.LogInformation(
                    "AMSI initialised as {AppName}. Uploads will be scanned before being written to disk.",
                    _options.AmsiApplicationName);
            }
            catch (DllNotFoundException ex)
            {
                IsAvailable = false;
                StatusMessage = "amsi.dll was not found on this host.";
                _logger.LogError(ex, "{Status}", StatusMessage);
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                StatusMessage = "AMSI could not be initialised: " + ex.Message;
                _logger.LogError(ex, "AMSI could not be initialised.");
            }
        }

        public async Task<ScanOutcome> ScanAsync(
            byte[] buffer, int length, string contentName, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (!IsAvailable || _context is null)
            {
                return new ScanOutcome(ScanVerdict.ScannerUnavailable, -1, StatusMessage);
            }

            if (length == 0)
            {
                return new ScanOutcome(ScanVerdict.Rejected, -1, "The file is empty.");
            }

            await _concurrencyGate.WaitAsync(cancellationToken);
            try
            {
                // AmsiScanBuffer is a blocking native call: run it off the request thread so a slow
                // provider does not tie up the ASP.NET thread pool thread handling the upload.
                return await Task.Run(() => ScanCore(buffer, length, contentName), cancellationToken);
            }
            finally
            {
                _concurrencyGate.Release();
            }
        }

        private ScanOutcome ScanCore(byte[] buffer, int length, string contentName)
        {
            var session = IntPtr.Zero;
            var sessionOpened = false;

            try
            {
                // A session is optional but recommended - without one the provider sees every
                // buffer as unrelated content.
                var sessionHr = AmsiNativeMethods.AmsiOpenSession(_context!, out session);
                if (sessionHr != 0)
                {
                    _logger.LogWarning(
                        "AmsiOpenSession failed with HRESULT 0x{Hr:X8}; scanning without a session.", sessionHr);
                    session = IntPtr.Zero;
                }
                else
                {
                    sessionOpened = true;
                }

                var scanHr = AmsiNativeMethods.AmsiScanBuffer(
                    _context!, buffer, (uint)length, contentName, session, out var result);

                if (scanHr != 0)
                {
                    var message = $"AmsiScanBuffer failed with HRESULT 0x{scanHr:X8}.";
                    _logger.LogError("{Message} Content: {ContentName}", message, contentName);
                    return new ScanOutcome(ScanVerdict.Error, -1, message);
                }

                if (AmsiResult.IsMalware(result))
                {
                    _logger.LogWarning(
                        "Malware detected in {ContentName}. AMSI_RESULT={Result}. The file was not saved.",
                        contentName, result);

                    return new ScanOutcome(
                        ScanVerdict.Malicious, result,
                        "The file was found to be malicious and has been rejected.");
                }

                if (AmsiResult.IsBlockedByAdmin(result))
                {
                    _logger.LogWarning(
                        "{ContentName} was blocked by administrator policy. AMSI_RESULT={Result}.",
                        contentName, result);

                    return new ScanOutcome(
                        ScanVerdict.BlockedByPolicy, result,
                        "The file was blocked by an administrator policy and has been rejected.");
                }

                return new ScanOutcome(ScanVerdict.Clean, result, "No threats were detected.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected failure scanning {ContentName}.", contentName);
                return new ScanOutcome(ScanVerdict.Error, -1, "The file could not be scanned.");
            }
            finally
            {
                if (sessionOpened)
                {
                    AmsiNativeMethods.AmsiCloseSession(_context!, session);
                }
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
            _concurrencyGate.Dispose();
        }
    }
}
