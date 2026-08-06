using RegulatoryReporting.Application;
using RegulatoryReporting.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<RegulatoryReportGenerator>();
builder.Services.AddSingleton<RegulatoryReportValidator>();
builder.Services.AddSingleton<RegulatorySubmissionService>();
builder.Services.AddSingleton<RegulatoryExportService>();
builder.Services.AddSingleton<RegulatoryReportAuditService>();
builder.Services.AddSingleton<IRegulatoryReportSigner, NoOpRegulatoryReportSigner>();
builder.Services.AddSingleton<RegulatoryReportService>();

var app = builder.Build();

app.MapPost("/api/v1/regulatory-reports", (CreateRegulatoryReportRequest request, RegulatoryReportService service) => Results.Ok(service.CreateReport(request)));
app.MapGet("/api/v1/regulatory-reports", (RegulatoryReportService service) => Results.Ok(service.ListReports()));
app.MapGet("/api/v1/regulatory-reports/{reportId}", (Guid reportId, RegulatoryReportService service) => Results.Ok(service.GetReport(reportId)));

app.MapPost("/api/v1/regulatory-reports/{reportId}/generate", (Guid reportId, ReportActionRequest request, RegulatoryReportService service) => Results.Ok(service.Generate(reportId, request)));
app.MapPost("/api/v1/regulatory-reports/{reportId}/review", (Guid reportId, ReportActionRequest request, RegulatoryReportService service) => Results.Ok(service.Review(reportId, request)));
app.MapPost("/api/v1/regulatory-reports/{reportId}/approve", (Guid reportId, ReportActionRequest request, RegulatoryReportService service) => Results.Ok(service.Approve(reportId, request)));
app.MapPost("/api/v1/regulatory-reports/{reportId}/submit", (Guid reportId, ReportActionRequest request, RegulatoryReportService service) => Results.Ok(service.Submit(reportId, request)));
app.MapPost("/api/v1/regulatory-reports/{reportId}/accept", (Guid reportId, ReportActionRequest request, RegulatoryReportService service) => Results.Ok(service.Accept(reportId, request)));
app.MapPost("/api/v1/regulatory-reports/{reportId}/reject", (Guid reportId, ReportActionRequest request, RegulatoryReportService service) => Results.Ok(service.Reject(reportId, request)));
app.MapPost("/api/v1/regulatory-reports/{reportId}/archive", (Guid reportId, ReportActionRequest request, RegulatoryReportService service) => Results.Ok(service.Archive(reportId, request)));

app.MapGet("/api/v1/regulatory-reports/{reportId}/versions", (Guid reportId, RegulatoryReportService service) => Results.Ok(service.GetVersions(reportId)));
app.MapGet("/api/v1/regulatory-reports/{reportId}/export", (Guid reportId, string format, RegulatoryReportService service) => Results.Ok(service.Export(reportId, format, "api-user")));
app.MapGet("/api/v1/regulatory-reports/{reportId}/submissions", (Guid reportId, RegulatoryReportService service) => Results.Ok(service.GetSubmissions(reportId)));

app.Run();
