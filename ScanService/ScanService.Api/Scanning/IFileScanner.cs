using ScanService.Client.Dtos;

namespace ScanService.Api.Scanning
{
    /// <summary>Scans an in-memory buffer and reports what the antimalware engine made of it.</summary>
    public interface IFileScanner
    {
        /// <summary>Name reported in scan results and on the health endpoint.</summary>
        string ProviderName { get; }

        /// <summary>True when AMSI initialised and an antimalware provider is registered.</summary>
        bool IsAvailable { get; }

        /// <summary>Why the scanner is unavailable, when it is.</summary>
        string StatusMessage { get; }

        /// <summary>
        /// Hands the buffer to the antimalware engine. <paramref name="contentName"/> should be the
        /// original file name - providers use it as a hint about how to interpret the content.
        /// </summary>
        Task<ScanOutcome> ScanAsync(byte[] buffer, int length, string contentName, CancellationToken cancellationToken);
    }

    /// <summary>Raw verdict from the engine, before it is turned into an API response.</summary>
    public sealed record ScanOutcome(ScanVerdict Verdict, int AmsiResultCode, string Message)
    {
        public bool IsClean => Verdict == ScanVerdict.Clean;
    }
}
