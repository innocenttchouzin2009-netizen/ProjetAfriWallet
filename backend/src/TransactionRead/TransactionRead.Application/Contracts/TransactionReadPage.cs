namespace AfriWallet.TransactionRead.Application.Contracts;

public sealed record TransactionReadPage(
    IReadOnlyList<TransactionReadItem> Items,
    TransactionReadCursor? NextCursor);
