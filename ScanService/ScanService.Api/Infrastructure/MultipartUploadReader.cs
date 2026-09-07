using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using ScanService.Api.Options;

namespace ScanService.Api.Infrastructure
{
    /// <summary>
    /// Pulls a multipart upload into memory without ever touching the file system.
    /// </summary>
    public sealed class MultipartUploadReader(IOptions<ScanServiceOptions> _options)
    {
        private readonly ScanServiceOptions _settings = _options.Value;

        public async Task<UploadReadResult> ReadAsync(HttpRequest request, CancellationToken cancellationToken)
        {
            if (!MultipartRequestHelper.IsMultipartContentType(request.ContentType))
            {
                return UploadReadResult.Invalid("The request must be a multipart/form-data upload.");
            }

            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(request.ContentType), _settings.MultipartBoundaryLengthLimit);

            var reader = new MultipartReader(boundary, request.Body);
            var formFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            byte[]? buffer = null;
            var length = 0;
            var fileName = string.Empty;
            string? contentType = null;

            var section = await reader.ReadNextSectionAsync(cancellationToken);
            while (section is not null)
            {
                var hasDisposition = ContentDispositionHeaderValue.TryParse(
                    section.ContentDisposition, out var contentDisposition);

                if (hasDisposition && MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                {
                    if (buffer is not null)
                    {
                        return UploadReadResult.Invalid("Send exactly one file per scan request.");
                    }

                    fileName = Path.GetFileName(
                        HeaderUtilities.RemoveQuotes(contentDisposition!.FileName).Value ?? string.Empty);
                    contentType = section.ContentType;

                    var copied = await CopyToMemoryAsync(section.Body, cancellationToken);
                    if (copied is null)
                    {
                        return UploadReadResult.TooBig(fileName, _settings.MaxFileSizeBytes);
                    }

                    (buffer, length) = copied.Value;
                }
                else if (hasDisposition && MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                {
                    var key = HeaderUtilities.RemoveQuotes(contentDisposition!.Name).Value;
                    if (!string.IsNullOrEmpty(key))
                    {
                        using var streamReader = new StreamReader(section.Body);
                        formFields[key] = await streamReader.ReadToEndAsync(cancellationToken);
                    }
                }

                section = await reader.ReadNextSectionAsync(cancellationToken);
            }

            if (buffer is null)
            {
                return UploadReadResult.Invalid("No file was included in the request.");
            }

            return UploadReadResult.Ok(buffer, length, fileName, contentType, formFields);
        }

        /// <summary>
        /// Copies the section body into a byte array, giving up as soon as the configured limit is
        /// passed. Returns null when the upload is too large.
        /// </summary>
        private async Task<(byte[] Buffer, int Length)?> CopyToMemoryAsync(
            Stream body, CancellationToken cancellationToken)
        {
            // Counting as we copy means a lying Content-Length header cannot make us buffer more
            // than the configured limit.
            using var memory = new MemoryStream();
            var chunk = new byte[81920];
            long total = 0;

            int read;
            while ((read = await body.ReadAsync(chunk, cancellationToken)) > 0)
            {
                total += read;
                if (total > _settings.MaxFileSizeBytes)
                {
                    return null;
                }

                await memory.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
            }

            return memory.TryGetBuffer(out var segment)
                ? (segment.Array!, (int)memory.Length)
                : (memory.ToArray(), (int)memory.Length);
        }
    }

    public sealed record UploadReadResult(
        bool Success,
        byte[]? Buffer,
        int Length,
        string FileName,
        string? ContentType,
        IReadOnlyDictionary<string, string> FormFields,
        string ErrorMessage,
        bool ExceededSizeLimit)
    {
        private static readonly Dictionary<string, string> Empty = new();

        public static UploadReadResult Ok(
            byte[] buffer, int length, string fileName, string? contentType,
            IReadOnlyDictionary<string, string> formFields) =>
            new(true, buffer, length, fileName, contentType, formFields, string.Empty, false);

        public static UploadReadResult Invalid(string message) =>
            new(false, null, 0, string.Empty, null, Empty, message, false);

        public static UploadReadResult TooBig(string fileName, long limitBytes) =>
            new(false, null, 0, fileName, null, Empty,
                $"The file is larger than the {limitBytes / (1024 * 1024)} MB limit.", true);

        public string? GetField(string name) =>
            FormFields.TryGetValue(name, out var value) ? value : null;
    }
}
