using Microsoft.Net.Http.Headers;

namespace ScanService.Api.Infrastructure
{
    /// <summary>
    /// Helpers for reading a multipart body by hand. Reading the body with a
    /// <see cref="Microsoft.AspNetCore.WebUtilities.MultipartReader"/> rather than binding
    /// <c>IFormFile</c> keeps ASP.NET Core from spooling the upload to a temporary file on disk,
    /// which is the whole point of this service.
    /// </summary>
    internal static class MultipartRequestHelper
    {
        internal static bool IsMultipartContentType(string? contentType) =>
            !string.IsNullOrEmpty(contentType) &&
            contentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase);

        internal static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

            if (string.IsNullOrWhiteSpace(boundary))
            {
                throw new InvalidDataException("Missing content-type boundary.");
            }

            if (boundary.Length > lengthLimit)
            {
                throw new InvalidDataException($"Multipart boundary length limit {lengthLimit} exceeded.");
            }

            return boundary;
        }

        internal static bool HasFileContentDisposition(ContentDispositionHeaderValue? contentDisposition) =>
            contentDisposition is not null &&
            contentDisposition.DispositionType.Equals("form-data") &&
            (!string.IsNullOrEmpty(contentDisposition.FileName.Value) ||
             !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));

        internal static bool HasFormDataContentDisposition(ContentDispositionHeaderValue? contentDisposition) =>
            contentDisposition is not null &&
            contentDisposition.DispositionType.Equals("form-data") &&
            string.IsNullOrEmpty(contentDisposition.FileName.Value) &&
            string.IsNullOrEmpty(contentDisposition.FileNameStar.Value);
    }
}
