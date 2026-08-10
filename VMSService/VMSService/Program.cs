using ConfigurationsService.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Client;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SmartBreadcrumbs.Extensions;
using System.Reflection;
using System.Text;
using VMSService.Core.Authorization;
using VMSService.Core.Static;
using VMSService.Core.Mappings;
using VMSService.Core.Models.Authentication;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.AgentDetails;
using VMSService.Core.Repositories.AgentDetailStatusHistories;
using VMSService.Core.Repositories.ApprovedCurrencyDenominations;
using VMSService.Core.Repositories.ApprovedCurrencyProcurements;
using VMSService.Core.Repositories.ApprovedCurrencyStatusHistories;
using VMSService.Core.Repositories.Attachments;
using VMSService.Core.Repositories.Bags;
using VMSService.Core.Repositories.CfbBoxes;
using VMSService.Core.Repositories.CfbCashHoldings;
using VMSService.Core.Repositories.CfbMovements;
using VMSService.Core.Repositories.CfbTopupRequests;
using VMSService.Core.Repositories.CfbTopupStatusHistories;
using VMSService.Core.Repositories.CfbUnitOfWorks;
using VMSService.Core.Repositories.BranchCategories;
using VMSService.Core.Repositories.Branches;
using VMSService.Core.Repositories.Categories;
using VMSService.Core.Repositories.CommercialBankBranches;
using VMSService.Core.Repositories.CommercialBanks;
using VMSService.Core.Repositories.CommercialBankUsers;
using VMSService.Core.Repositories.CounterPartyTypes;
using VMSService.Core.Repositories.BPSMachines;
using VMSService.Core.Repositories.CountingRooms;
using VMSService.Core.Repositories.CountingRoomSessions;
using VMSService.Core.Repositories.StockProcessingPlans;
using VMSService.Core.Repositories.StockProcessingPlanStatusHistories;
using VMSService.Core.Repositories.StockProcessingSessions;
using VMSService.Core.Repositories.StockProcessingSessionStatusHistories;
using VMSService.Core.Repositories.StockProcessingResults;
using VMSService.Core.Repositories.CurrencyVendors;
using VMSService.Core.Repositories.ProcessingBoards;
using VMSService.Core.Repositories.Denominations;
using VMSService.Core.Repositories.DenominationTypes;
using VMSService.Core.Repositories.DepositFees;
using VMSService.Core.Repositories.DepositProcessingFees;
using VMSService.Core.Repositories.DepositUnitOfWorks;
using VMSService.Core.Repositories.ExpectedCurrencies;
using VMSService.Core.Repositories.ExPrintMintDetails;
using VMSService.Core.Repositories.ExPrintMintStatusHistories;
using VMSService.Core.Repositories.FeeTypes;
using VMSService.Core.Repositories.Logistics;
using VMSService.Core.Repositories.MinDepositStandards;
using VMSService.Core.Repositories.NewStockMovementLogs;
using VMSService.Core.Repositories.PackingListRangeDetails;
using VMSService.Core.Repositories.PackingListRanges;
using VMSService.Core.Repositories.AppliedPenalties;
using VMSService.Core.Repositories.Penalties;
using VMSService.Core.Repositories.PenaltyWaivers;
using VMSService.Core.Repositories.PublicHolidays;
using VMSService.Core.Repositories.ReceivedStocks;
using VMSService.Core.Repositories.StockTakes;
using VMSService.Core.Repositories.StockTakeStatusHistories;
using VMSService.Core.Repositories.StockTakeSystemPositions;
using VMSService.Core.Repositories.ReceivedStocksDetails;
using VMSService.Core.Repositories.ReceivedStockStatusHistories;
using VMSService.Core.Repositories.StockMovementDiscrepancies;
using VMSService.Core.Repositories.StockMovements;
using VMSService.Core.Repositories.StockMovementStatusHistories;
using VMSService.Core.Repositories.Stocks;
using VMSService.Core.Repositories.SubCategories;
using VMSService.Core.Repositories.VendorTypes;
using VMSService.Core.Repositories.WithdrawalRequestAgents;
using VMSService.Core.Repositories.WithdrawalRequestDenominations;
using VMSService.Core.Repositories.WithdrawalRequests;
using VMSService.Core.Repositories.WithdrawalRequestStatusHistories;
using VMSService.Core.Repositories.WithdrawalTransactionRefNumberGenerators;
using VMSService.Core.Repositories.WithdrawalUnitOfWorks;
using VMSService.Core.Repository.Vaults;
using VMSService.Core.Repository.VaultSections;
using VMSService.Core.Services.Configurations;
using VMSService.Core.Services.Identity;
using VMSService.Core.Services.PackingListRangeDetails;
using VMSService.Core.Services.StockPositions;
using VMSService.Core.Services.Stocks;
using VMSService.Core.Services.Workflows;
using VMSService.Data;
using WorkflowService.Client;
using VMSService.Core.Repositories.BoxStates;
using VMSService.Core.Repositories.DestructionRequests;
using VMSService.Core.Repositories.DestructionRequestStatusHistories;
using VMSService.Core.Repositories.DestructionTypes;
using VMSService.Core.Repositories.DestructionAuthorizations;
using VMSService.Core.Repositories.DestructionAuthorizationStatusHistories;
using VMSService.Core.Repositories.DestructionLocations;
using VMSService.Core.Repositories.EodDiscrepancyReports;
using VMSService.Core.Repositories.EodDiscrepancyReportStatusHistories;
using VMSService.Core.Repositories.MonthlySurpluses;
using VMSService.Core.Repositories.MonthlySurplusStatusHistories;
using VMSService.Core.Repositories.MonthlyShortages;
using VMSService.Core.Repositories.MonthlyShortageStatusHistories;
using VMSService.Core.Repositories.MonthlyShortageReimbursements;
using VMSService.Core.Repositories.MonthlyCommercialBankSettlements;
using VMSService.Core.Repositories.MonthlyCommercialBankSettlementStatusHistories;
using VMSService.Core.Repositories.BankingSurpluses;
using VMSService.Core.Repositories.BankingSurplusStatusHistories;
using VMSService.Core.Repositories.BoardSessionSurplusShortageStatusHistories;
using VMSService.Core.Repositories.BoardSessionSurplusShortages;
using VMSService.Core.Repositories.NoteExaminers;
using VMSService.Core.Repositories.BankingShortages;
using VMSService.Core.Repositories.BankingShortageStatusHistories;
using VMSService.Web.Services;
using VMSService.Core.Repositories.BagMovements;
using VMSService.Core.Repositories.BagMovementStatusHistories;
using VMSService.Core.Repositories.MIandFits;

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
                context.NoResult();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Redirect(authenticationSettings?.LoginPath ?? "/logout");
                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Redirect(authenticationSettings?.LoginPath ?? "/logout");
                await context.Response.CompleteAsync();
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.Redirect("/Home/AccessDenied");
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
builder.Services.AddScoped<IBPSMachineRepository, BPSMachineRepository>();
builder.Services.AddScoped<IProcessingBoardRepository, ProcessingBoardRepository>();
builder.Services.AddScoped<ICountingRoomRepository, CountingRoomRepository>();
builder.Services.AddScoped<IStockProcessingPlanRepository, StockProcessingPlanRepository>();
builder.Services.AddScoped<ICountingRoomSessionRepository, CountingRoomSessionRepository>();
builder.Services.AddScoped<IStockProcessingPlanStatusHistoryRepository, StockProcessingPlanStatusHistoryRepository>();
builder.Services.AddScoped<IStockProcessingSessionRepository, StockProcessingSessionRepository>();
builder.Services.AddScoped<IStockProcessingSessionStatusHistoryRepository, StockProcessingSessionStatusHistoryRepository>();
builder.Services.AddScoped<IReceivedStockRepository, ReceivedStockRepository>();
builder.Services.AddScoped<IReceivedStockDetailsRepository, ReceivedStockDetailsRepository>();
builder.Services.AddScoped<IPackingListDetailService, PackingListDetailService>();
builder.Services.AddScoped<IWorkflowService, WorkflowServices>();
builder.Services.AddScoped<IWithdrawalStatusResolver, WithdrawalStatusResolver>();
builder.Services.AddScoped<IReceivedStockStatusHistoryRepository, ReceivedStockStatusHistoryRepository>();
builder.Services.AddScoped<IStockMovementStatusHistoryRepository, StockMovementStatusHistoryRepository>();
builder.Services.AddScoped<IStockLedgerRepository, StockLedgerRepository>();
builder.Services.AddScoped<IPackingListRangeRepository, PackingListRangeRepository>();
builder.Services.AddScoped<IPackingListDetailsRepository, PackingListDetailsRepository>();
builder.Services.AddScoped<IStockLedgerDetailRepository, StockLedgerDetailRepository>();
builder.Services.AddScoped<ICommercialBankRepository, CommercialBankRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
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
builder.Services.AddScoped<IDestructionRequestRepository, DestructionRequestRepository>();
builder.Services.AddScoped<IDestructionRequestStatusHistoryRepository, DestructionRequestStatusHistoryRepository>();
builder.Services.AddScoped<IDestructionTypeRepository, DestructionTypeRepository>();
builder.Services.AddScoped<IDestructionLocationRepository, DestructionLocationRepository>();
builder.Services.AddScoped<IDestructionAuthorizationRepository, DestructionAuthorizationRepository>();
builder.Services.AddScoped<IDestructionAuthorizationStatusHistoryRepository, DestructionAuthorizationStatusHistoryRepository>();
builder.Services.AddScoped<IEodDiscrepancyReportRepository, EodDiscrepancyReportRepository>();
builder.Services.AddScoped<IEodDiscrepancyReportStatusHistoryRepository, EodDiscrepancyReportStatusHistoryRepository>();
builder.Services.AddScoped<IMonthlySurplusRepository, MonthlySurplusRepository>();
builder.Services.AddScoped<IMonthlySurplusStatusHistoryRepository, MonthlySurplusStatusHistoryRepository>();
builder.Services.AddScoped<IMonthlyShortageRepository, MonthlyShortageRepository>();
builder.Services.AddScoped<IMonthlyShortageDetailRepository, MonthlyShortageDetailRepository>();
builder.Services.AddScoped<IMonthlyShortageStatusHistoryRepository, MonthlyShortageStatusHistoryRepository>();
builder.Services.AddScoped<IMonthlyShortageReimbursementRepository, MonthlyShortageReimbursementRepository>();
builder.Services.AddScoped<IMonthlyShortageReimbursementStatusHistoryRepository, MonthlyShortageReimbursementStatusHistoryRepository>();
builder.Services.AddScoped<IMonthlyCommercialBankSettlementRepository, MonthlyCommercialBankSettlementRepository>();
builder.Services.AddScoped<IMonthlyCommercialBankSettlementStatusHistoryRepository, MonthlyCommercialBankSettlementStatusHistoryRepository>();
builder.Services.AddScoped<IBankingSurplusRepository, BankingSurplusRepository>();
builder.Services.AddScoped<IBankingSurplusStatusHistoryRepository, BankingSurplusStatusHistoryRepository>();
builder.Services.AddScoped<IBoardSessionSurplusShortageStatusHistoryRepository, BoardSessionSurplusShortageStatusHistoryRepository>();
builder.Services.AddScoped<IBoardSessionSurplusShortageRepository, BoardSessionSurplusShortageRepository>();
builder.Services.AddScoped<INoteExaminerRepository, NoteExaminerRepository>();
builder.Services.AddScoped<IBankingShortageRepository, BankingShortageRepository>();
builder.Services.AddScoped<IBankingShortageStatusHistoryRepository, BankingShortageStatusHistoryRepository>();
builder.Services.AddScoped<IAttachmentRepository, AttachmentRepository>();
builder.Services.AddScoped<ICfbBoxRepository, CfbBoxRepository>();
builder.Services.AddScoped<ICfbCashHoldingRepository, CfbCashHoldingRepository>();
builder.Services.AddScoped<ICfbMovementRepository, CfbMovementRepository>();
builder.Services.AddScoped<ITopupRequestRepository, TopupRequestRepository>();
builder.Services.AddScoped<ITopupRequestStatusHistoryRepository, TopupRequestStatusHistoryRepository>();
builder.Services.AddScoped<ICfbUnitOfWork, CfbUnitOfWork>();
builder.Services.AddScoped<IPenaltyRepository, PenaltyRepository>();
builder.Services.AddScoped<IAppliedPenaltyRepository, AppliedPenaltyRepository>();
builder.Services.AddScoped<IPenaltyWaiverRepository, PenaltyWaiverRepository>();
builder.Services.AddScoped<IPublicHolidayRepository, PublicHolidayRepository>();
builder.Services.AddScoped<IStockTakeRepository, StockTakeRepository>();
builder.Services.AddScoped<IStockTakeStatusHistoryRepository, StockTakeStatusHistoryRepository>();
builder.Services.AddScoped<IStockTakeSystemPositionRepository, StockTakeSystemPositionRepository>();

// ── Resource-based ownership check ────────────────────────────
builder.Services.AddSingleton<IAuthorizationHandler, CbOwnershipAuthorizationHandler>();
builder.Services.AddScoped<ILogisticsRepository, LogisticsRepository>();
builder.Services.AddScoped<ILogisticsStatusHistoryRepository, LogisticsStatusHistoryRepository>();
builder.Services.AddScoped<INewStockMovementLogRepository, NewStockMovementLogRepository>();

// Add this in your service registration section
builder.Services.AddScoped<IStockService, VMSService.Core.Services.Stocks.StockService>();
builder.Services.AddScoped<IStockPositionService, StockPositionService>();

builder.Services.AddScoped<IAttachmentRepository, AttachmentRepository>();
builder.Services.AddScoped<IBagRepository, BagRepository>();
// Add after line with builder.Services.AddScoped<IBagRepository, BagRepository>();
builder.Services.AddScoped<IVaultRepository, VaultRepository>();
builder.Services.AddScoped<IVaultSectionRepository, VaultSectionRepository>();
builder.Services.AddScoped<IBagMovementRepository, BagMovementRepository>();
builder.Services.AddScoped<IBagMovementStatusHistoryRepository, BagMovementStatusHistoryRepository>();
builder.Services.AddScoped<IMIandFitRepository, MIandFitRepository>();
builder.Services.AddScoped<IStockProcessingResultRepository, StockProcessingResultRepository>();
builder.Services.AddScoped<IMinDepositStandardRepository, MinDepositStandardRepository>();
builder.Services.AddScoped<IDepositProcessingFeeRepository, DepositProcessingFeeRepository>();
builder.Services.AddScoped<IDepositFeeRepository, DepositFeeRepository>();
builder.Services.AddScoped<IFeeTypeRepository, FeeTypeRepository>();
builder.Services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();

builder.Services.AddHttpClient<VMSService.Core.Services.Stocks.StockService>()
    .ConfigureHttpClient((sp, client) =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["StockApiSettings:BaseUrl"] ?? "https://localhost:7158";
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

// Register Permission Service (cached lookup against IdentityService)
builder.Services.AddHttpClient<IPermissionService, PermissionService>()
    .ConfigureHttpClient((sp, client) =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["MicroServices:identityservice"] ?? "https://localhost:7026";
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
        var baseUrl = configuration["MicroServices:configurationsservice"] ?? "https://localhost:7089";
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

// Register Workflow Service
var workflowServiceBaseUrl = builder.Configuration["MicroServices:workflowservice"] ?? "https://localhost:7288";
builder.Services.AddWorkflowServiceClient(workflowServiceBaseUrl);

// Register ConfigurationsServiceClient Service
builder.Services.AddConfigurationsServiceClient(builder.Configuration["MicroServices:configurationsservice"] ?? "https://localhost:7089");

// Register Notification Service
builder.Services.AddNotificationServiceClient(builder.Configuration["MicroServices:notificationservice"] ?? "https://localhost:7129");
var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitMqUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
var rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";
builder.Services.AddNotificationEventPublisher(rabbitMqHost, rabbitMqUsername, rabbitMqPassword);
builder.Services.AddNotificationServiceClient(builder.Configuration["MicroServices:notificationservice"] ?? "https://localhost:7026");

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

builder.Services.AddHostedService<VMSService.Web.BackgroundServices.WithdrawalExpiryService>();

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

Directory.CreateDirectory(FileStorageConstants.UploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(FileStorageConstants.UploadsRoot),
    RequestPath = "/uploads"
});

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
