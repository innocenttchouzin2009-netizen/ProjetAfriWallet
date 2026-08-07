using Reconciliation.Application.Interfaces;
using Reconciliation.Application.Matching;
using Reconciliation.Application.Services;
using Reconciliation.Contracts.Requests;
using Reconciliation.Infrastructure.DataSources;
using Reconciliation.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<
	SandboxReconciliationDataSource>();

builder.Services.AddSingleton<
	IReconciliationDataSource>(
		sp => sp.GetRequiredService<
			SandboxReconciliationDataSource>());

builder.Services.AddSingleton<
	IReconciliationRepository,
	InMemoryReconciliationRepository>();

builder.Services.AddSingleton(
	new ReconciliationMatcher(
		TimeSpan.FromMinutes(10)));

builder.Services.AddScoped<
	ReconciliationService>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet(
	"/health/live",
	() => Results.Ok(new
	{
		status = "Healthy",
		service = "afriwallet-reconciliation"
	}));

app.MapPost(
	"/api/v1/reconciliation/runs",
	async (
		StartReconciliationRequest request,
		ReconciliationService service,
		CancellationToken cancellationToken) =>
	{
		var run =
			await service.RunAsync(
				request.PartnerId,
				request.PeriodStartUtc,
				request.PeriodEndUtc,
				cancellationToken);

		return Results.Created(
			$"/api/v1/reconciliation/runs/{run.RunId}",
			run);
	});

app.MapGet(
	"/api/v1/reconciliation/runs/{runId:guid}",
	async (
		Guid runId,
		IReconciliationRepository repository,
		CancellationToken cancellationToken) =>
	{
		var run =
			await repository.GetRunAsync(
				runId,
				cancellationToken);

		return run is null
			? Results.NotFound()
			: Results.Ok(run);
	});

app.MapOpenApi();

app.Run();

public partial class Program;
