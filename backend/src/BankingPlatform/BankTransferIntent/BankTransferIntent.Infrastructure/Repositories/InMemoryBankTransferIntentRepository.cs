using System.Collections.Concurrent;
using AfriWallet.BankingPlatform.BankTransferIntent.Application.Interfaces;
using TransferIntent = AfriWallet.BankingPlatform.BankTransferIntent.Domain.Transfers.BankTransferIntent;

namespace AfriWallet.BankingPlatform.BankTransferIntent.Infrastructure.Repositories;

public sealed class InMemoryBankTransferIntentRepository
    : IBankTransferIntentRepository
{
    private readonly ConcurrentDictionary<Guid, TransferIntent> _items = new();

    public Task AddAsync(
        TransferIntent transferIntent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_items.Values.Any(x =>
                string.Equals(
                    x.IdempotencyKey,
                    transferIntent.IdempotencyKey,
                    StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                "Bank transfer idempotency key already exists.");
        }

        if (!_items.TryAdd(
                transferIntent.TransferIntentId,
                transferIntent))
        {
            throw new InvalidOperationException(
                "Bank transfer intent already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<TransferIntent?> GetAsync(
        Guid transferIntentId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _items.TryGetValue(
            transferIntentId,
            out var transferIntent);

        return Task.FromResult(
            transferIntent);
    }

    public Task<TransferIntent?>
        GetByIdempotencyKeyAsync(
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var transfer =
            _items.Values.FirstOrDefault(x =>
                string.Equals(
                    x.IdempotencyKey,
                    idempotencyKey,
                    StringComparison.Ordinal));

        return Task.FromResult(
            transfer);
    }

    public Task<IReadOnlyCollection<TransferIntent>>
        ListByOwnerAsync(
            string ownerAwid,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<
            IReadOnlyCollection<TransferIntent>>(
            _items.Values
                .Where(x =>
                    string.Equals(
                        x.OwnerAwid,
                        ownerAwid,
                        StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(
                    x => x.CreatedAtUtc)
                .ToArray());
    }
}
