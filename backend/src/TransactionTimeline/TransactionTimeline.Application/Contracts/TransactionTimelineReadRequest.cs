namespace AfriWallet.TransactionTimeline.Application.Contracts;

public sealed record TransactionTimelineReadRequest(
    Guid WalletId,
    int Limit = 50,
    DateTimeOffset? Before = null);
