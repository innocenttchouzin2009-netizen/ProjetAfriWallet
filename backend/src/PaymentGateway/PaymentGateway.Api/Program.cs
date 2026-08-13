using PaymentGateway.Api.Api;
using PaymentGateway.Api.Application;
using PaymentGateway.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IConnectorResolver, ConnectorResolver>();
builder.Services.AddSingleton<IExecutionRepository, InMemoryExecutionRepository>();
builder.Services.AddSingleton<RetryScheduler>();
builder.Services.AddSingleton<IExecuteTransferHandler, ExecuteTransferHandler>();
builder.Services.AddSingleton<PaymentStateTransitionValidator>();
builder.Services.AddSingleton<IPaymentTimelineRepository, InMemoryPaymentTimelineRepository>();
builder.Services.AddSingleton<IPaymentEventPublisher, InMemoryPaymentEventPublisher>();
builder.Services.AddSingleton<GatewayAuthenticationService>();
builder.Services.AddSingleton<GatewayRateLimiter>();
builder.Services.AddSingleton<PaymentOrchestrator>();

var app = builder.Build();
app.MapTransferExecutionEndpoints();
app.MapPaymentOrchestrationEndpoints();
app.MapPublicGatewayEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();
