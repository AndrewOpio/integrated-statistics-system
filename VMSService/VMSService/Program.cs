using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SmartBreadcrumbs.Extensions;
using System.Reflection;
using System.Text;
using VMSService.Core.Mappings;
using VMSService.Core.Repositories.ApprovedCurrencyDenominations;
using VMSService.Core.Repositories.ApprovedCurrencyProcurements;
using VMSService.Core.Repositories.ApprovedCurrencyStatusHistories;
using VMSService.Core.Repositories.CurrencyVendors;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.DenominationTypes;
using VMSService.Core.Repositories.ExpectedCurrencies;
using VMSService.Core.Repositories.ExPrintMintStatusHistories;
using VMSService.Core.Repositories.ReceivedStocks;
using VMSService.Core.Repositories.ReceivedStockStatusHistories;
using VMSService.Core.Repositories.StatusTypes;
using VMSService.Core.Repositories.VendorTypes;
using VMSService.Core.Repositories.WorkflowTransitions;
using VMSService.Core.Services.Workflows;
using VMSService.Data;

var builder = WebApplication.CreateBuilder(args);

// Service Discovery
builder.Services.AddServiceDiscovery();

builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler();
    http.AddServiceDiscovery();
});

// Create NAMED HttpClient with service name as base address
// IMPORTANT: Use "http://vmsservice" - service discovery will resolve this
builder.Services.AddHttpClient("VMSService", client =>
{
    // Use the service name - Aspire will resolve it
    client.BaseAddress = new Uri("http://vmsservice");
});

// Add OpenTelemetry
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

var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
if (useOtlpExporter)
{
    builder.Services.AddOpenTelemetry().UseOtlpExporter();
}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// Make IHttpContextAccessor available for injection, TransactionContext to log the user
builder.Services.AddHttpContextAccessor();

DataServicesRegistration.AddDataServices(builder.Services, builder.Configuration);

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
    .SetApplicationName("GatewayService");

// Configure AutoMapper
builder.Services.AddAutoMapper(cfg => { }, typeof(VMSServiceAutoMapperProfile));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };

        // For MVC views, check token from cookie
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.ContainsKey("AuthToken"))
                {
                    context.Token = context.Request.Cookies["AuthToken"];
                }
                return Task.CompletedTask;
            }
        };
    });

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

// Register Repositories
builder.Services.AddScoped<IVendorTypeRepository, VendorTypeRepository>();
builder.Services.AddScoped<IStatusTypeRepository, StatusTypeRepository>();
builder.Services.AddScoped<IExPrintMintStatusHistoryRepository, ExPrintMintStatusHistoryRepository>();
builder.Services.AddScoped<IApprovedCurrencyStatusHistoryRepository, ApprovedCurrencyStatusHistoryRepository>();
builder.Services.AddScoped<IDenominationRepository, DenominationRepository>();
builder.Services.AddScoped<IDenominationTypeRepository, DenominationTypeRepository>();
builder.Services.AddScoped<IExPrintMintRepository, ExPrintMintRepository>();
builder.Services.AddScoped<IApprovedCurrencyProcurementRepository, ApprovedCurrencyProcurementRepository>();
builder.Services.AddScoped<IApprovedCurrencyDenominationRepository, ApprovedCurrencyDenominationRepository>();
builder.Services.AddScoped<ICurrencyVendorRepository, CurrencyVendorRepository>();
builder.Services.AddScoped<IReceivedStockRepository, ReceivedStockRepository>();
builder.Services.AddScoped<IWorkflowTransitionRepository, WorkflowTransitionRepository>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IReceivedStockStatusHistoryRepository, ReceivedStockStatusHistoryRepository>();

// Add SmartBreadcrumbs
builder.Services.AddBreadcrumbs(Assembly.GetExecutingAssembly(), options =>
{
    options.TagName = "nav";
    options.TagClasses = "";
    options.OlClasses = "breadcrumb breadcrumb-transparent";
    options.LiClasses = "breadcrumb-item";
    options.ActiveLiClasses = "breadcrumb-item active";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHealthChecks("/alive", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});

app.Run();
