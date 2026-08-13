namespace AfriWallet.BankingPlatform.BankTransferIntent.Application;

public sealed record CreateBankTransferIntentRequest(
    string OwnerAwid,
    Guid BeneficiaryId,
    Guid BankAccountId,
    long AmountMinor,
    string CurrencyCode,
    string Reference,
    string IdempotencyKey,
    int LifetimeMinutes);

public sealed record BankTransferIntentView(
    Guid TransferIntentId,
    string OwnerAwid,
    Guid BeneficiaryId,
    Guid BankAccountId,
    long AmountMinor,
    string CurrencyCode,
    string Reference,
    string Status,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc);
