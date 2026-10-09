using MobileMoney.Production.Payout.Execution.Abstractions;
using MobileMoney.Production.Payout.Execution.Domain;

namespace MobileMoney.Production.Payout.Execution.Application;

public sealed class MobileMoneyPayoutExecutionResolver
{
    private readonly IMobileMoneyPayoutExecutionQuoteReader _quoteReader;
    private readonly IMobileMoneyPayoutExecutionIdempotencyStore _idempotencyStore;

    public MobileMoneyPayoutExecutionResolver(
        IMobileMoneyPayoutExecutionQuoteReader quoteReader,
        IMobileMoneyPayoutExecutionIdempotencyStore idempotencyStore)
    {
        _quoteReader = quoteReader
            ?? throw new ArgumentNullException(nameof(quoteReader));
        _idempotencyStore = idempotencyStore
            ?? throw new ArgumentNullException(nameof(idempotencyStore));
    }

    public async Task<MobileMoneyPayoutExecutionResolution> ResolveAsync(
        MobileMoneyPayoutExecutionIntent intent,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);

        if (nowUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Timestamp must be UTC.", nameof(nowUtc));

        var existing = await _idempotencyStore.FindAsync(
            intent.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
            return ResolveExisting(existing, intent);

        var quote = await _quoteReader.FindAsync(
            intent.QuoteId,
            cancellationToken);

        if (quote is null)
        {
            throw new MobileMoneyPayoutExecutionException(
                MobileMoneyPayoutExecutionErrorCodes.QuoteNotFound);
        }

        var binding = MobileMoneyPayoutExecutionBinding.Create(
            quote,
            intent,
            nowUtc);

        var reservation = new MobileMoneyPayoutExecutionIdempotencyEntry(
            intent.IdempotencyKey,
            intent.Fingerprint,
            intent.QuoteId,
            null,
            nowUtc);

        if (await _idempotencyStore.TryCreateAsync(
                reservation,
                cancellationToken))
        {
            return MobileMoneyPayoutExecutionResolution.Execute(
                binding,
                reservation);
        }

        var winner = await _idempotencyStore.FindAsync(
            intent.IdempotencyKey,
            cancellationToken);

        if (winner is null)
        {
            throw new InvalidOperationException(
                "Idempotency reservation collision did not expose the winning entry.");
        }

        return ResolveExisting(winner, intent);
    }

    private static MobileMoneyPayoutExecutionResolution ResolveExisting(
        MobileMoneyPayoutExecutionIdempotencyEntry existing,
        MobileMoneyPayoutExecutionIntent intent)
    {
        if (!existing.Matches(intent))
        {
            throw new MobileMoneyPayoutExecutionException(
                MobileMoneyPayoutExecutionErrorCodes.IdempotencyConflict);
        }

        return MobileMoneyPayoutExecutionResolution.Replay(existing);
    }
}
