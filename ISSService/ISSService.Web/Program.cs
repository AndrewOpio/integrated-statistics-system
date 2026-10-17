using ISSService.Core.Services.Workflows;
using ISSService.Data.Configurations;
using ISSService.Data.Services.Workflows;
using ISSService.Web.Services.Extraction;
using ISSService.Web.Services.Sacco;
using ISSService.Web.Services.Submissions;
using ISSService.Web.Services.Templates;
using ISSService.Web.Services.Validation;
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

// Register ISS scanned submission storage.
// Submitted returns are scanned by the Bank's ScanService before
// any file bytes are written to ISS storage.
builder.Services.AddScoped<
    IScannedSubmissionStorageService,
    ScannedSubmissionStorageService>();

// Register the shared SACCO workbook reader.
// Validation and operational-data extraction use the same
// low-level Open XML workbook interpretation.
builder.Services.AddScoped<
    ISaccoWorkbookReader,
    SaccoWorkbookReader>();

// Register SACCO workbook validation.
// The validator independently evaluates the submitted workbook
// rather than relying on the workbook's own validation formulas.
builder.Services.AddScoped<
    ISaccoWorkbookValidationService,
    SaccoWorkbookValidationService>();

// Register submission validation orchestration.
// This service creates auditable validation runs, persists findings,
// and updates the submission validation status.
builder.Services.AddScoped<
    ISubmissionValidationService,
    SubmissionValidationService>();

// Register SACCO operational-data extraction.
// Successfully validated SACCO returns are transformed into the
// generic operational dataset and statistical observation model.
builder.Services.AddScoped<
    ISaccoDataExtractionService,
    SaccoDataExtractionService>();

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