using Microsoft.Extensions.DependencyInjection;

namespace ScanService.Client
{
    public static class ScanServiceClientExtensions
    {
        /// <summary>
        /// Registers the scan service client. Antimalware scans take far longer than an ordinary
        /// API call, so the timeout is generous by default; override it for very large uploads.
        /// </summary>
        public static IHttpClientBuilder AddScanServiceClient(
            this IServiceCollection services, string baseurl, TimeSpan? timeout = null)
        {
            var builder = services.AddHttpClient<IScanServiceClient, ScanServiceClient>(client =>
            {
                client.BaseAddress = new Uri(baseurl.EndsWith('/') ? baseurl : baseurl + "/");
                client.Timeout = timeout ?? TimeSpan.FromMinutes(3);
            });

            // Several Aswam hosts apply a standard resilience handler to every HttpClient. Its
            // 10 second per-attempt timeout is far too short for a scan, and its retries would try
            // to replay an upload stream that has already been consumed - so this client opts out
            // and relies on the timeout above instead.
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is marked experimental upstream.
            builder.RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

            return builder;
        }
    }
}
