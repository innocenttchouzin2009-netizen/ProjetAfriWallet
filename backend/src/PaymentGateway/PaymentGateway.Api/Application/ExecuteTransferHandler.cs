using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Infrastructure;

namespace PaymentGateway.Api.Application;

public interface IExecuteTransferHandler
{
    ExecutionResult Execute(Guid transferIntentId, string providerCode, string transferType, string correlationId, string traceId);
    ExecutionResult Retry(Guid executionId);
    ExecutionResult Cancel(Guid executionId);
    TransferExecution? Get(Guid executionId);
    IReadOnlyCollection<TransferExecution> List();
}

public sealed class ExecuteTransferHandler : IExecuteTransferHandler
{
    private readonly IExecutionRepository _repository;
    private readonly IConnectorResolver _resolver;

    public ExecuteTransferHandler(IExecutionRepository repository, IConnectorResolver resolver)
    {
        _repository = repository;
        _resolver = resolver;
    }

    public ExecutionResult Execute(Guid transferIntentId, string providerCode, string transferType, string correlationId, string traceId)
    {
        var resolution = _resolver.Resolve(providerCode, transferType);
        var execution = new TransferExecution(Guid.NewGuid(), transferIntentId, resolution.ProviderCode, resolution.ConnectorType, resolution.ExecutionMode, correlationId, traceId, DateTimeOffset.UtcNow);
        execution.Start();
        execution.MarkSent($"{resolution.ProviderCode}-{execution.Id:N}");
        execution.MarkAccepted();
        execution.MarkProcessing();
        execution.MarkSettled();
        execution.Complete();

        _repository.Save(execution);
        return ToResult(execution);
    }

    public ExecutionResult Retry(Guid executionId)
    {
        var existing = _repository.Get(executionId) ?? throw new InvalidOperationException("EXECUTION_NOT_FOUND");

        var retryExecution = new TransferExecution(
            Guid.NewGuid(),
            existing.TransferIntentId,
            existing.ProviderCode,
            existing.ConnectorType,
            existing.ExecutionMode,
            existing.CorrelationId,
            existing.TraceId,
            DateTimeOffset.UtcNow);

        retryExecution.RecordRetry();
        retryExecution.Start();
        retryExecution.MarkSent($"retry-{retryExecution.Id:N}");
        retryExecution.MarkAccepted();
        retryExecution.MarkProcessing();
        retryExecution.MarkSettled();
        retryExecution.Complete();

        _repository.Save(retryExecution);
        return ToResult(retryExecution);
    }

    public ExecutionResult Cancel(Guid executionId)
    {
        var execution = _repository.Get(executionId) ?? throw new InvalidOperationException("EXECUTION_NOT_FOUND");
        execution.Cancel();
        _repository.Save(execution);
        return ToResult(execution);
    }

    public TransferExecution? Get(Guid executionId) => _repository.Get(executionId);

    public IReadOnlyCollection<TransferExecution> List() => _repository.List();

    private static ExecutionResult ToResult(TransferExecution execution) => new(
        execution.Id,
        execution.Status,
        execution.ConnectorType,
        execution.ProviderCode,
        execution.ProviderReference ?? string.Empty,
        execution.RetryCount,
        execution.DurationMs ?? 0,
        execution.FailureReason,
        execution.UpdatedAt);
}
