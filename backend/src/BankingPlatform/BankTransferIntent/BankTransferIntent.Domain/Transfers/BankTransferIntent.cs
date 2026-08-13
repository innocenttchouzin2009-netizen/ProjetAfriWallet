using AfriWallet.BankingPlatform.BankTransferIntent.Domain.Money;

namespace AfriWallet.BankingPlatform.BankTransferIntent.Domain.Transfers;

public sealed class BankTransferIntent
{
    public BankTransferIntent(
        Guid transferIntentId,
        string ownerAwid,
        Guid beneficiaryId,
        Guid bankAccountId,
        MoneyAmount amount,
        string reference,
        string idempotencyKey,
        DateTime expiresAtUtc)
    {
        if (transferIntentId == Guid.Empty)
            throw new ArgumentException(
                "Transfer intent ID is required.");

        if (beneficiaryId == Guid.Empty)
            throw new ArgumentException(
                "Beneficiary ID is required.");

        if (bankAccountId == Guid.Empty)
            throw new ArgumentException(
                "Bank account ID is required.");

        if (expiresAtUtc <= DateTime.UtcNow)
            throw new ArgumentException(
                "Expiration must be in the future.");

        TransferIntentId = transferIntentId;
        OwnerAwid = Require(ownerAwid);
        BeneficiaryId = beneficiaryId;
        BankAccountId = bankAccountId;
        Amount = amount;
        Reference = Require(reference);
        IdempotencyKey = Require(idempotencyKey);
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid TransferIntentId { get; }

    public string OwnerAwid { get; }

    public Guid BeneficiaryId { get; }

    public Guid BankAccountId { get; }

    public MoneyAmount Amount { get; }

    public string Reference { get; }

    public string IdempotencyKey { get; }

    public BankTransferIntentStatus Status { get; private set; }
        = BankTransferIntentStatus.Created;

    public DateTime CreatedAtUtc { get; }
        = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; }

    public DateTime? ConfirmedAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public bool IsExpired(DateTime nowUtc) =>
        nowUtc >= ExpiresAtUtc;

    public void Confirm()
    {
        EnsureNotExpired();

        if (Status != BankTransferIntentStatus.Created)
            throw new InvalidOperationException(
                "Only created bank transfer intents may be confirmed.");

        Status = BankTransferIntentStatus.Confirmed;
        ConfirmedAtUtc = DateTime.UtcNow;
    }

    public void MarkReadyForRouting()
    {
        EnsureNotExpired();

        if (Status != BankTransferIntentStatus.Confirmed)
            throw new InvalidOperationException(
                "Only confirmed transfer intents can become ready for routing.");

        Status = BankTransferIntentStatus.ReadyForRouting;
    }

    public void Cancel()
    {
        if (Status is
            BankTransferIntentStatus.Completed or
            BankTransferIntentStatus.Failed or
            BankTransferIntentStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Finalized transfer intent cannot be cancelled.");
        }

        Status = BankTransferIntentStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
    }

    public void Expire()
    {
        if (!IsExpired(DateTime.UtcNow))
            throw new InvalidOperationException(
                "Transfer intent has not expired.");

        if (Status is
            BankTransferIntentStatus.Completed or
            BankTransferIntentStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Finalized transfer intent cannot expire.");
        }

        Status = BankTransferIntentStatus.Expired;
    }

    private void EnsureNotExpired()
    {
        if (IsExpired(DateTime.UtcNow))
            throw new InvalidOperationException(
                "Bank transfer intent has expired.");
    }

    private static string Require(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Value is required.");

        return value.Trim();
    }
}

public enum BankTransferIntentStatus
{
    Created,
    Confirmed,
    ReadyForRouting,
    Processing,
    Completed,
    Failed,
    Cancelled,
    Expired
}
