using System.Collections.Concurrent;
using AfriWallet.BankingPlatform.BankTransferExecution.Application.Interfaces;
using Execution =
    AfriWallet.BankingPlatform.BankTransferExecution.Domain.Executions.BankTransferExecution;

namespace AfriWallet.BankingPlatform.BankTransferExecution.Infrastructure.Repositories;

public sealed class InMemoryBankTransferExecutionRepository
    : IBankTransferExecutionRepository
{
    private readonly ConcurrentDictionary<Guid, Execution> _items = new();

    public Task AddAsync(
        Execution execution,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_items.Values.Any(x =>
                string.Equals(
                    x.IdempotencyKey,
                    execution.IdempotencyKey,
                    StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                "Execution idempotency key already exists.");
        }

        if (!_items.TryAdd(
                execution.ExecutionId,
                execution))
        {
            throw new InvalidOperationException(
                "Bank transfer execution already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<Execution?> GetAsync(
        Guid executionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _items.TryGetValue(
            executionId,
            out var execution);

        return Task.FromResult(execution);
    }

    public Task<Execution?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(
            _items.Values.FirstOrDefault(x =>
                string.Equals(
                    x.IdempotencyKey,
                    idempotencyKey,
                    StringComparison.Ordinal)));
    }
}
