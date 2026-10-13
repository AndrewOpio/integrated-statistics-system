using ISSService.Core.Services.Workflows;
using ISSService.Data.Configurations;
using ISSService.Data.Services.Workflows;
using ISSService.Web.Services.Templates;
using Microsoft.EntityFrameworkCore;
using ScanService.Client;
using WorkflowService.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register ISS database context.
builder.Services.AddDbContext<ISSServiceDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Register the Bank's shared Workflow Service client.
var workflowServiceBaseUrl =
    builder.Configuration["MicroServices:workflowservice"]
    ?? "https://localhost:7288";

builder.Services.AddWorkflowServiceClient(workflowServiceBaseUrl);

// Register ISS respondent-case workflow integration.
builder.Services.AddScoped<
    IRespondentCaseWorkflowService,
    RespondentCaseWorkflowService>();

// Register the Bank's shared Scan Service client.
// All user-supplied files must be scanned before ISS writes them to disk.
var scanServiceBaseUrl =
    builder.Configuration["MicroServices:scanservice"]
    ?? "https://localhost:7315";

builder.Services.AddScanServiceClient(scanServiceBaseUrl);

// Register ISS scanned template storage.
// The service stores only bytes returned as clean by ScanService.
builder.Services.AddScoped<
    IScannedTemplateStorageService,
    ScannedTemplateStorageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    // The default HSTS value is 30 days.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();