namespace AfriWallet.PaymentRequests.Application;

public sealed record ExpireDuePaymentRequestsCommand(
    DateTimeOffset AsOfUtc,
    int BatchSize = 100);

public sealed record ExpireDuePaymentRequestsResult(
    int Scanned,
    int Expired,
    int Skipped);
