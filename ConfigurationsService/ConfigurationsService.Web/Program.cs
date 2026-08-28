using ConfigurationsService.Core;
using ConfigurationsService.Core.Mappings;
using ConfigurationsService.Core.Models.Authentication;
using ConfigurationsService.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using SmartBreadcrumbs.Extensions;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Service Discovery
builder.Services.AddServiceDiscovery();

builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler();
    http.AddServiceDiscovery();
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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Make IHttpContextAccessor available for injection, TransactionContext to log the user
builder.Services.AddHttpContextAccessor();

DataServicesRegistration.AddDataServices(builder.Services, builder.Configuration);

// Configure AutoMapper
builder.Services.AddAutoMapper(cfg => { }, typeof(ConfigurationsAutoMapperProfile));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
var authenticationSettings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>();

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
                // NEVER redirect from here. This event runs during authentication,
                // which happens on EVERY request that carries an AuthToken cookie -
                // including anonymous pages. Redirecting here sent the browser to the
                // gateway login page, which re-sent the same expired cookie and was
                // redirected again: ERR_TOO_MANY_REDIRECTS.
                // Just record the failure and let OnChallenge decide, since that only
                // runs for endpoints that actually require authorization.
                context.NoResult();
                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                // Fires only when a protected endpoint rejected the request.
                context.HandleResponse();

                // Clear the stale token, otherwise the browser keeps replaying it.
                // Options must match how the cookie was issued or the delete is ignored.
                context.Response.Cookies.Delete("AuthToken", new CookieOptions
                {
                    Path = "/",
                    Domain = string.IsNullOrWhiteSpace(authenticationSettings?.CookieDomain)
                        ? null
                        : authenticationSettings.CookieDomain,
                    Secure = true,
                    SameSite = SameSiteMode.None
                });

                context.Response.Redirect(authenticationSettings?.LoginPath ?? "/Login");
                await context.Response.CompleteAsync();
            }


        };
    });

// Add CORS for microservices
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

//Add configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

CoreServicesRegistration.AddCoreServices(builder.Services);

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
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

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

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});

app.Run();
