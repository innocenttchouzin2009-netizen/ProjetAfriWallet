using AfriWallet.BankingPlatform.BankTransferExecution.Application.Interfaces;
using AfriWallet.BankingPlatform.BankTransferExecution.Domain.Executions;
using Execution = AfriWallet.BankingPlatform.BankTransferExecution.Domain.Executions.BankTransferExecution;

namespace AfriWallet.BankingPlatform.BankTransferExecution.Application.Services;

public sealed class BankTransferExecutionService
{
    private readonly IBankTransferExecutionRepository _repository;
    private readonly ITransferIntentGateway _transferIntents;
    private readonly IBankRoutingGateway _routing;
    private readonly IBankProviderGateway _providers;

    public BankTransferExecutionService(
        IBankTransferExecutionRepository repository,
        ITransferIntentGateway transferIntents,
        IBankRoutingGateway routing,
        IBankProviderGateway providers)
    {
        _repository = repository;
        _transferIntents = transferIntents;
        _routing = routing;
        _providers = providers;
    }

    public async Task<Execution> ExecuteAsync(
        ExecuteBankTransferRequest request,
        CancellationToken cancellationToken)
    {
        var existing =
            await _repository.GetByIdempotencyKeyAsync(
                request.IdempotencyKey,
                cancellationToken);

        if (existing is not null)
            return existing;

        var intent =
            await _transferIntents.GetEligibilityAsync(
                request.TransferIntentId,
                cancellationToken);

        if (!intent.Exists)
            throw new InvalidOperationException(
                "Bank transfer intent does not exist.");

        if (!intent.ReadyForRouting)
            throw new InvalidOperationException(
                "Bank transfer intent is not ready for execution.");

        if (intent.AmountMinor != request.AmountMinor)
            throw new InvalidOperationException(
                "Transfer amount does not match intent.");

        if (!string.Equals(
                intent.CurrencyCode,
                request.CurrencyCode,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Transfer currency does not match intent.");
        }

        var route =
            await _routing.GetDecisionAsync(
                request.RoutingDecisionId,
                cancellationToken);

        if (!route.Exists)
            throw new InvalidOperationException(
                "Bank routing decision does not exist.");

        if (!string.Equals(
                route.ProviderCode,
                request.ProviderCode,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                route.RailCode,
                request.RailCode,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Execution request does not match routing decision.");
        }

        var execution =
            new Execution(
                Guid.NewGuid(),
                request.TransferIntentId,
                request.RoutingDecisionId,
                request.ProviderCode,
                request.RailCode,
                request.AmountMinor,
                request.CurrencyCode,
                request.IdempotencyKey);

        await _repository.AddAsync(
            execution,
            cancellationToken);

        execution.Start();

        var result =
            await _providers.ExecuteAsync(
                execution.ExecutionId,
                execution.ProviderCode,
                execution.RailCode,
                execution.AmountMinor,
                execution.CurrencyCode,
                cancellationToken);

        if (!result.Success)
        {
            execution.Fail(
                result.ErrorCode ?? "provider_failure");

            return execution;
        }

        execution.MarkSubmitted(
            result.ProviderReference ??
            throw new InvalidOperationException(
                "Provider reference missing."));

        return execution;
    }

    public async Task<Execution> CompleteAsync(
        Guid executionId,
        CancellationToken cancellationToken)
    {
        var execution =
            await _repository.GetAsync(
                executionId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Bank transfer execution not found.");

        execution.Complete();

        return execution;
    }

    public Task<Execution?> GetAsync(
        Guid executionId,
        CancellationToken cancellationToken)
    {
        return _repository.GetAsync(
            executionId,
            cancellationToken);
    }
}
