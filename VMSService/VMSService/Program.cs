using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Client;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SmartBreadcrumbs.Extensions;
using System.Reflection;
using System.Text;
using VMSService.Core.Authorization;
using VMSService.Core.Mappings;
using VMSService.Core.Models.Authentication;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.AgentDetailStatusHistories;
using VMSService.Core.Repositories.ApprovedCurrencyDenominations;
using VMSService.Core.Repositories.ApprovedCurrencyProcurements;
using VMSService.Core.Repositories.ApprovedCurrencyStatusHistories;
using VMSService.Core.Repositories.BranchCategories;
using VMSService.Core.Repositories.Branches;
using VMSService.Core.Repositories.Categories;
using VMSService.Core.Repositories.CommercialBankBranches;
using VMSService.Core.Repositories.CommercialBanks;
using VMSService.Core.Repositories.CommercialBankUsers;
using VMSService.Core.Repositories.CounterPartyTypes;
using VMSService.Core.Repositories.CurrencyVendors;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.DenominationTypes;
using VMSService.Core.Repositories.DepositUnitOfWorks;
using VMSService.Core.Repositories.ExpectedCurrencies;
using VMSService.Core.Repositories.ExPrintMintStatusHistories;
using VMSService.Core.Repositories.ExPrintMintDetails;
using VMSService.Core.Repositories.ReceivedStocks;
using VMSService.Core.Repositories.ReceivedStocksDetails;
using VMSService.Core.Repositories.ReceivedStockStatusHistories;
using VMSService.Core.Repositories.StockMovementDiscrepancies;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Repositories.StockMovementStatusHistories;
using VMSService.Core.Repositories.Logistics;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Repositories.VendorTypes;
using VMSService.Core.Repositories.WithdrawalRequestAgents;
using VMSService.Core.Repositories.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalRequestStatusHistories;
using VMSService.Core.Repositories.WithdrawalTransactionRefNumberGenerators;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Services.Configurations;
using VMSService.Core.Services.Identity;
using VMSService.Core.Services.StockPositions;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Workflows;
using VMSService.Data;
using VMSService.Core.Repositories.PackingListRangeDetails;
using VMSService.Core.Repositories.PackingListRanges;
using VMSService.Core.Services.PackingListRangeDetails;
using WorkflowService.Client;

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

//builder.Services
//    .AddDataProtection()
//    .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
//    .SetApplicationName("GatewayService");

// Configure AutoMapper
builder.Services.AddAutoMapper(cfg => { }, typeof(VMSServiceAutoMapperProfile));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
var authenticationSettings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>();



builder.Services.AddAuthorizationBuilder()
    .AddPolicy(RolePolicy.CbOnly, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(RolePolicy.CbMaker, RolePolicy.CbApprover))
    .AddPolicy(RolePolicy.BouOnly, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(RolePolicy.BouChecker, RolePolicy.PanelA, RolePolicy.PanelB));


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
            },
            OnAuthenticationFailed = context =>
            {
                // Handle specific authentication failures
                context.NoResult();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Redirect(authenticationSettings?.LoginPath ?? "/logout");
                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                // This event fires when authentication fails
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Redirect(authenticationSettings?.LoginPath ?? "/logout");
                await context.Response.CompleteAsync();
            }
            

        };
    });

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

//Add configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// Register Repositories
builder.Services.AddScoped<IVendorTypeRepository, VendorTypeRepository>();
builder.Services.AddScoped<IExPrintMintStatusHistoryRepository, ExPrintMintStatusHistoryRepository>();
builder.Services.AddScoped<IApprovedCurrencyStatusHistoryRepository, ApprovedCurrencyStatusHistoryRepository>();
builder.Services.AddScoped<IDenominationRepository, DenominationRepository>();
builder.Services.AddScoped<IDenominationTypeRepository, DenominationTypeRepository>();
builder.Services.AddScoped<IExPrintMintRepository, ExPrintMintRepository>();
builder.Services.AddScoped<IExPrintMintDetailRepository, ExPrintMintDetailRepository>();
builder.Services.AddScoped<IApprovedCurrencyProcurementRepository, ApprovedCurrencyProcurementRepository>();
builder.Services.AddScoped<IApprovedCurrencyDenominationRepository, ApprovedCurrencyDenominationRepository>();
builder.Services.AddScoped<ICurrencyVendorRepository, CurrencyVendorRepository>();
builder.Services.AddScoped<IReceivedStockRepository, ReceivedStockRepository>();
builder.Services.AddScoped<IReceivedStockDetailsRepository, ReceivedStockDetailsRepository>();
builder.Services.AddScoped<IPackingListDetailService, PackingListDetailService>();
builder.Services.AddScoped<IWorkflowService, WorkflowServices>();
builder.Services.AddScoped<IReceivedStockStatusHistoryRepository, ReceivedStockStatusHistoryRepository>();
builder.Services.AddScoped<IStockMovementStatusHistoryRepository, StockMovementStatusHistoryRepository>();
builder.Services.AddScoped<IStockLedgerRepository, StockLedgerRepository>();
builder.Services.AddScoped<IPackingListRangeRepository, PackingListRangeRepository>();
builder.Services.AddScoped<IPackingListDetailsRepository, PackingListDetailsRepository>();
builder.Services.AddScoped<IStockLedgerDetailRepository, StockLedgerDetailRepository>();
builder.Services.AddScoped<ICommercialBankRepository, CommercialBankRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IBranchCategoryRepository, BranchCategoryRepository>();
builder.Services.AddScoped<IBranchRepository, BranchRepository>();
builder.Services.AddScoped<ICommercialBankBranchRepository, CommercialBankBranchRepository>();
builder.Services.AddScoped<ICounterPartyTypeRepository, CounterPartyTypeRepository>();
builder.Services.AddScoped<ICommercialBankUserRepository, CommercialBankUserRepository>();
builder.Services.AddScoped<IAgentDetailRepository, AgentDetailRepository>();
builder.Services.AddScoped<IStockMovementRepository, StockMovementRepository>();
builder.Services.AddScoped<IStockMovementDiscrepancyRepository, StockMovementDiscrepancyRepository>();
builder.Services.AddScoped<IWithdrawalRequestRepository, WithdrawalRequestRepository>();
builder.Services.AddScoped<IWithdrawalRequestAgentRepository, WithdrawalRequestAgentRepository>();
builder.Services.AddScoped<IWithdrawalRequestDenominationRepository, WithdrawalRequestDenominationRepository>();
builder.Services.AddScoped<IWithdrawalRequestStatusHistoryRepository, WithdrawalRequestStatusHistoryRepository>();
builder.Services.AddScoped<IWithdrawalTransactionRefNumberGenerator, WithdrawalTransactionRefNumberGenerator>();
builder.Services.AddScoped<IWithdrawalUnitOfWork, WithdrawalUnitOfWork>();
builder.Services.AddScoped<IAgentDetailStatusHistoryRepository, AgentDetailStatusHistoryRepository>();
builder.Services.AddScoped<IDepositUnitOfWork, DepositUnitOfWork>();
builder.Services.AddScoped<IDepositTransactionRefNumberGenerator, DepositTransactionRefNumberGenerator>();
builder.Services.AddScoped<IDepositRequestRepository, DepositRequestRepository>();
builder.Services.AddScoped<IDepositRequestStatusHistoryRepository, DepositRequestStatusHistoryRepository>();

// ── Resource-based ownership check ────────────────────────────
builder.Services.AddSingleton<IAuthorizationHandler, CbOwnershipAuthorizationHandler>();
builder.Services.AddScoped<ILogisticsRepository, LogisticsRepository>();
builder.Services.AddScoped<ILogisticsStatusHistoryRepository, LogisticsStatusHistoryRepository>();

// Add this in your service registration section
builder.Services.AddScoped<IStockService, VMSService.Core.Services.Stocks.StockService>();
builder.Services.AddScoped<IStockPositionService, StockPositionService>();
builder.Services.AddHttpClient<VMSService.Core.Services.Stocks.StockService>()
    .ConfigureHttpClient((sp, client) =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["StockApiSettings:BaseUrl"] ?? "https://localhost:7158";
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

// Register Identity Service
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddHttpClient<IdentityService>()
    .ConfigureHttpClient((sp, client) =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["MicroServices:identityservice"] ?? "https://localhost:7026";
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

// Register Configurations Service
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

builder.Services.AddHttpClient<ConfigurationService>()
    .ConfigureHttpClient((sp, client) =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["MicroServices:configurationsservice"] ?? "https://localhost:7026";
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

// Register Workflow Service
var workflowServiceBaseUrl = builder.Configuration["MicroServices:workflowservice"] ?? "https://localhost:7288";
builder.Services.AddWorkflowServiceClient(workflowServiceBaseUrl);

// Register ConfigurationsServiceClient Service
builder.Services.AddConfigurationsServiceClient(builder.Configuration["MicroServices:configurationsservice"] ?? "https://localhost:7026");

// Register Notification Service
builder.Services.AddNotificationServiceClient(builder.Configuration["MicroServices:notificationservice"] ?? "https://localhost:7026");
var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitMqUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";
builder.Services.AddNotificationEventPublisher(rabbitMqHost, rabbitMqUsername, rabbitMqPassword);

// Configure LDAP settings
builder.Services.Configure<StockAPISettings>(
    builder.Configuration.GetSection("StockApiSettings"));

// Add SmartBreadcrumbs
builder.Services.AddBreadcrumbs(Assembly.GetExecutingAssembly(), options =>
{
    options.TagName = "nav";
    options.TagClasses = "";
    options.OlClasses = "breadcrumb breadcrumb-transparent";
    options.LiClasses = "breadcrumb-item";
    options.ActiveLiClasses = "breadcrumb-item active";
});

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHealthChecks("/alive", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});

app.Run();
