using Liquidity.Application.Interfaces;
using Liquidity.Application.Services;
using Liquidity.Contracts.Requests;
using Liquidity.Domain.Thresholds;
using Liquidity.Infrastructure.ReadModels;
using Liquidity.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ITreasuryReadModel, InMemoryTreasuryReadModel>();
builder.Services.AddSingleton<ILiquiditySnapshotRepository, InMemoryLiquiditySnapshotRepository>();
builder.Services.AddScoped<LiquidityManagementService>();

var app = builder.Build();

app.MapGet("/api/v1/liquidity/positions", async (LiquidityManagementService service, CancellationToken cancellationToken) =>
{
	var positions = await service.GetPositionsAsync(cancellationToken);
	return Results.Ok(positions);
});

app.MapGet("/api/v1/liquidity/accounts/{id:guid}", async (Guid id, LiquidityManagementService service, CancellationToken cancellationToken) =>
{
	var position = await service.GetAccountPositionAsync(id, cancellationToken);
	return Results.Ok(position);
});

app.MapGet("/api/v1/liquidity/snapshots", async (LiquidityManagementService service, CancellationToken cancellationToken) =>
{
	var snapshots = await service.GetSnapshotsAsync(cancellationToken);
	return Results.Ok(snapshots);
});

app.MapPost("/api/v1/liquidity/rebalance", async (RebalanceLiquidityRequest request, LiquidityManagementService service, CancellationToken cancellationToken) =>
{
	var threshold = new LiquidityThreshold
	{
		MinimumMinor = request.MinimumMinor,
		WarningMinor = request.WarningMinor,
		CriticalMinor = request.CriticalMinor
	};

	var result = await service.ProposeRebalanceAsync(request.CurrencyCode, threshold, cancellationToken);
	return Results.Ok(result);
});

app.MapGet("/api/v1/liquidity/alerts", async (LiquidityManagementService service, CancellationToken cancellationToken) =>
{
	var threshold = new LiquidityThreshold
	{
		MinimumMinor = 2_000_000,
		WarningMinor = 1_200_000,
		CriticalMinor = 700_000
	};

	var alerts = await service.GetAlertsAsync(threshold, cancellationToken);
	return Results.Ok(alerts);
});

app.MapGet("/api/v1/liquidity/forecast", async (string currencyCode, LiquidityManagementService service, CancellationToken cancellationToken) =>
{
	var forecast = await service.GetForecastAsync(currencyCode, cancellationToken);
	return Results.Ok(forecast);
});

app.Run();

public partial class Program;
