namespace AfriWallet.Merchant.Application.Contracts.QrPayments;

public sealed record QrTimelineResponse(
    string TransferIntentId,
    IReadOnlyList<string> Items);
