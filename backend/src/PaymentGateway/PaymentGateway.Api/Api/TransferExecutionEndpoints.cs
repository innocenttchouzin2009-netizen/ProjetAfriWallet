using PaymentGateway.Api.Application;
using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Api;

public static class TransferExecutionEndpoints
{
    public static void MapTransferExecutionEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/payment-executions", (CreateExecutionRequest request, IExecuteTransferHandler handler) =>
        {
            if (string.IsNullOrWhiteSpace(request.ProviderCode))
            {
                return Results.BadRequest(new { code = "PROVIDER_CODE_REQUIRED", message = "Provider code is required." });
            }

            var result = handler.Execute(request.TransferIntentId, request.ProviderCode, request.TransferType, request.CorrelationId ?? Guid.NewGuid().ToString("N"), request.TraceId ?? Guid.NewGuid().ToString("N"));
            return Results.Created($"/api/v1/payment-executions/{result.ExecutionId}", result);
        });

        app.MapGet("/api/v1/payment-executions/{executionId:guid}", (Guid executionId, IExecuteTransferHandler handler) =>
        {
            var execution = handler.Get(executionId);
            return execution is null
                ? Results.NotFound(new { code = "EXECUTION_NOT_FOUND", message = "Execution not found." })
                : Results.Ok(ToResponse(execution));
        });

        app.MapGet("/api/v1/payment-executions", (IExecuteTransferHandler handler) =>
        {
            var items = handler.List().Select(ToResponse).ToList();
            return Results.Ok(new { items });
        });

        app.MapPost("/api/v1/payment-executions/{executionId:guid}/retry", (Guid executionId, IExecuteTransferHandler handler) =>
        {
            try
            {
                return Results.Ok(handler.Retry(executionId));
            }
            catch (InvalidOperationException ex) when (ex.Message == "EXECUTION_NOT_FOUND")
            {
                return Results.NotFound(new { code = ex.Message, message = "Execution not found." });
            }
            catch (InvalidOperationException ex) when (ex.Message == "EXECUTION_TERMINAL")
            {
                return Results.Conflict(new { code = ex.Message, message = "Execution is already terminal." });
            }
        });

        app.MapPost("/api/v1/payment-executions/{executionId:guid}/cancel", (Guid executionId, IExecuteTransferHandler handler) =>
        {
            try
            {
                return Results.Ok(handler.Cancel(executionId));
            }
            catch (InvalidOperationException ex) when (ex.Message == "EXECUTION_NOT_FOUND")
            {
                return Results.NotFound(new { code = ex.Message, message = "Execution not found." });
            }
        });
    }

    private static object ToResponse(TransferExecution execution) => new
    {
        execution.Id,
        execution.TransferIntentId,
        execution.ProviderCode,
        execution.ConnectorType,
        execution.ExecutionMode,
        execution.Status,
        execution.StartedAt,
        execution.CompletedAt,
        execution.DurationMs,
        execution.RetryCount,
        execution.FailureReason,
        execution.ProviderReference,
        execution.CorrelationId,
        execution.TraceId,
        execution.CreatedAt,
        execution.UpdatedAt,
        execution.Version
    };

    public sealed record CreateExecutionRequest(
        Guid TransferIntentId,
        string ProviderCode,
        string TransferType,
        string? CorrelationId,
        string? TraceId);
}
