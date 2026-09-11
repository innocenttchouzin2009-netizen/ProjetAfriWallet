using Reporting.Application.Interfaces;
using Reporting.Application.Services;
using Reporting.Contracts.Responses;
using Reporting.Infrastructure.DataSources;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IReportingDataSource, InMemoryReportingDataSource>();
builder.Services.AddScoped<ReportingDashboardService>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet("/health/live", () =>
    Results.Ok(new
    {
        status = "Healthy",
        service = "afriwallet-reporting-platform"
    }));

app.MapGet(
    "/api/v1/reporting/dashboard/executive",
    async (
        DateTime? fromUtc,
        DateTime? toUtc,
        ReportingDashboardService service,
        CancellationToken cancellationToken) =>
    {
        var end = toUtc ?? DateTime.UtcNow;
        var start = fromUtc ?? end.AddDays(-30);

        var snapshot = await service.BuildExecutiveDashboardAsync(start, end, cancellationToken);

        var response = new ExecutiveDashboardResponse(
            snapshot.GeneratedAtUtc,
            snapshot.Metrics.Select(metric => new MetricResponse(
                metric.MetricCode,
                metric.DisplayName,
                metric.Value,
                metric.Unit)).ToArray(),
            snapshot.Alerts.Select(alert => new AlertResponse(
                alert.Code,
                alert.Severity,
                alert.Message,
                alert.OccurredAtUtc)).ToArray());

        return Results.Ok(response);
    });

app.MapGet("/api/v1/reporting/reports", () => Results.Ok(Array.Empty<object>()));
app.MapPost("/api/v1/reporting/reports", () => Results.Accepted());
app.MapOpenApi();
app.Run();

public partial class Program;
