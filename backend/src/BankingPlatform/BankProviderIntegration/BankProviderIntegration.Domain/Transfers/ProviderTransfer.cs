namespace AfriWallet.BankingPlatform.BankProviderIntegration.Domain.Transfers;

public sealed class ProviderTransfer
{
    public ProviderTransfer(
        Guid providerTransferId,
        Guid executionId,
        string providerCode,
        string railCode,
        long amountMinor,
        string currencyCode,
        string idempotencyKey)
    {
        if (providerTransferId == Guid.Empty)
            throw new ArgumentException("Provider transfer ID is required.");

        if (executionId == Guid.Empty)
            throw new ArgumentException("Execution ID is required.");

        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor));

        ProviderTransferId = providerTransferId;
        ExecutionId = executionId;
        ProviderCode = Require(providerCode).ToUpperInvariant();
        RailCode = Require(railCode).ToUpperInvariant();
        AmountMinor = amountMinor;
        CurrencyCode = NormalizeCurrency(currencyCode);
        IdempotencyKey = Require(idempotencyKey);
    }

    public Guid ProviderTransferId { get; }

    public Guid ExecutionId { get; }

    public string ProviderCode { get; }

    public string RailCode { get; }

    public long AmountMinor { get; }

    public string CurrencyCode { get; }

    public string IdempotencyKey { get; }

    public ProviderTransferStatus Status { get; private set; }
        = ProviderTransferStatus.Created;

    public string? ProviderReference { get; private set; }

    public DateTime CreatedAtUtc { get; }
        = DateTime.UtcNow;

    public DateTime? SubmittedAtUtc { get; private set; }

    public void MarkSubmitting()
    {
        if (Status != ProviderTransferStatus.Created)
            throw new InvalidOperationException("Transfer must be Created.");

        Status = ProviderTransferStatus.Submitting;
    }

    public void MarkSubmitted(string providerReference)
    {
        if (Status != ProviderTransferStatus.Submitting)
            throw new InvalidOperationException("Transfer must be Submitting.");

        ProviderReference = Require(providerReference);
        Status = ProviderTransferStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        if (Status == ProviderTransferStatus.Submitted)
            throw new InvalidOperationException(
                "Submitted transfer cannot be rewritten as failed.");

        Status = ProviderTransferStatus.Failed;
    }

    private static string NormalizeCurrency(string value)
    {
        var currency = Require(value).ToUpperInvariant();
        if (currency.Length != 3)
            throw new ArgumentException("Currency must use ISO 4217.");

        return currency;
    }

    private static string Require(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.");

        return value.Trim();
    }
}

public enum ProviderTransferStatus
{
    Created,
    Submitting,
    Submitted,
    Failed
}
