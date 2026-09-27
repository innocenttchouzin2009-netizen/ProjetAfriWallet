using AfriWallet.TransactionTimeline.Application.Contracts;

namespace AfriWallet.TransactionTimeline.Application.Sources;

public interface ILedgerTransactionTimelineSource
{
    Task<TransactionTimelinePage> ReadAsync(
        TransactionTimelineReadRequest request,
        CancellationToken cancellationToken = default);
}
