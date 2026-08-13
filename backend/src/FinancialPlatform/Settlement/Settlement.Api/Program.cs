using Settlement.Application.Interfaces;
using Settlement.Application.Services;
using Settlement.Contracts.Requests;
using Settlement.Infrastructure.Gateways;
using Settlement.Infrastructure.Providers;
using Settlement.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ISettlementRepository, InMemorySettlementRepository>();
builder.Services.AddSingleton<IFxQuoteProvider, SandboxFxQuoteProvider>();
builder.Services.AddSingleton<ITreasurySettlementGateway, SandboxTreasurySettlementGateway>();
builder.Services.AddScoped<MultiCurrencySettlementService>();
builder.Services.AddScoped<SettlementPositionService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Settlement.Api" }));

app.MapPost(
	"/api/v1/settlement/instructions",
	async (CreateSettlementInstructionRequest request, MultiCurrencySettlementService service, CancellationToken cancellationToken) =>
	{
		var instruction = await service.CreateInstructionAsync(
			request.SourceAccountId,
			request.DestinationAccountId,
			request.SourceCurrency,
			request.DestinationCurrency,
			request.SourceAmountMinor,
			cancellationToken);

		return Results.Created($"/api/v1/settlement/instructions/{instruction.InstructionId}", instruction);
	});

app.MapPost(
	"/api/v1/settlement/instructions/{instructionId:guid}/execute",
	async (Guid instructionId, MultiCurrencySettlementService service, CancellationToken cancellationToken) =>
	{
		var instruction = await service.ExecuteInstructionAsync(instructionId, cancellationToken);
		return Results.Ok(instruction);
	});

app.MapGet(
	"/api/v1/settlement/instructions/{instructionId:guid}",
	async (Guid instructionId, MultiCurrencySettlementService service, CancellationToken cancellationToken) =>
	{
		var instruction = await service.GetInstructionAsync(instructionId, cancellationToken);
		return instruction is null ? Results.NotFound() : Results.Ok(instruction);
	});

app.MapPost(
	"/api/v1/settlement/batches",
	async (CreateSettlementBatchRequest request, MultiCurrencySettlementService service, CancellationToken cancellationToken) =>
	{
		var batch = await service.CreateBatchAsync(request.InstructionIds, cancellationToken);
		return Results.Created($"/api/v1/settlement/batches/{batch.BatchId}", batch);
	});

app.MapPost(
	"/api/v1/settlement/batches/{batchId:guid}/execute",
	async (Guid batchId, MultiCurrencySettlementService service, CancellationToken cancellationToken) =>
	{
		var batch = await service.ExecuteBatchAsync(batchId, cancellationToken);
		return Results.Ok(batch);
	});

app.MapGet(
	"/api/v1/settlement/positions",
	async (SettlementPositionService service, CancellationToken cancellationToken) =>
	{
		var positions = await service.GetPositionsAsync(cancellationToken);
		return Results.Ok(positions);
	});

app.MapGet(
	"/api/v1/settlement/quotes",
	async (string from, string to, long amountMinor, IFxQuoteProvider quoteProvider, CancellationToken cancellationToken) =>
	{
		var quote = await quoteProvider.GetQuoteAsync(from, to, amountMinor, cancellationToken);
		return Results.Ok(quote);
	});

app.Run();
