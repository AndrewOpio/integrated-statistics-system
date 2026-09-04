using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ScanService.Api.Infrastructure;
using ScanService.Api.Options;
using ScanService.Api.Scanning;
using ScanService.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

// Service Discovery, so the other Aswam services can address this one as "http://scanservice".
builder.Services.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler();
    http.AddServiceDiscovery();
});

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    });

if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    builder.Services.AddOpenTelemetry().UseOtlpExporter();
}

builder.Services.Configure<ScanServiceOptions>(
    builder.Configuration.GetSection(ScanServiceOptions.SectionName));

var scanOptions = builder.Configuration
    .GetSection(ScanServiceOptions.SectionName)
    .Get<ScanServiceOptions>() ?? new ScanServiceOptions();

// Keep uploads in memory rather than letting ASP.NET Core spool them to a temp file. The scan
// endpoints read the multipart body themselves, but this covers anything else that touches a form.
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = scanOptions.MaxFileSizeBytes + 1024 * 1024;
    options.MemoryBufferThreshold = int.MaxValue;
    options.ValueLengthLimit = int.MaxValue;
});

builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = scanOptions.MaxFileSizeBytes + 1024 * 1024;
});

// One AMSI context for the process, sessions opened per scan.
builder.Services.AddSingleton<IFileScanner, AmsiFileScanner>();
builder.Services.AddSingleton<IScannedFileStore, ScannedFileStore>();
builder.Services.AddSingleton<MultipartUploadReader>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Aswam Scan Service", Version = "v1" });
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "ScanService.Api.xml"), true);

    // The scan actions read the multipart body themselves, so Swagger has no parameters to build a
    // file picker from. This declares that body for them.
    c.OperationFilter<MultipartUploadOperationFilter>();
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
    .AddCheck<AmsiHealthCheck>("amsi", tags: ["ready"]);

var app = builder.Build();

// Surface a bad configuration at start-up rather than on the first upload.
var scanner = app.Services.GetRequiredService<IFileScanner>();
if (!scanner.IsAvailable)
{
    app.Logger.LogCritical(
        "The antimalware scanner is not available: {Status} Uploads will be {Behaviour}.",
        scanner.StatusMessage,
        scanOptions.FailClosed ? "rejected" : "accepted WITHOUT scanning (ScanService:FailClosed is false)");
}

if (scanOptions.AllowedDestinationRoots.Count == 0)
{
    app.Logger.LogCritical(
        "ScanService:AllowedDestinationRoots is empty, so the store endpoint will refuse every request.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Scan Service v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/alive", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready")
});

app.Run();

/// <summary>Reports the service unhealthy when AMSI could not be initialised.</summary>
internal sealed class AmsiHealthCheck(IFileScanner _scanner) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(_scanner.IsAvailable
            ? HealthCheckResult.Healthy(_scanner.StatusMessage)
            : HealthCheckResult.Unhealthy(_scanner.StatusMessage));
}
