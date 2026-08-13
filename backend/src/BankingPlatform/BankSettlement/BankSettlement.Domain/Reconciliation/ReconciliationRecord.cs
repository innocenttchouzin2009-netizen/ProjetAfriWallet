namespace AfriWallet.BankingPlatform.BankSettlement.Domain.Reconciliation;

public sealed class ReconciliationRecord
{
    public ReconciliationRecord(
        Guid reconciliationId,
        Guid settlementBatchId,
        long expectedAmountMinor,
        long reportedAmountMinor,
        string currencyCode,
        string? externalReference)
    {
        if (reconciliationId == Guid.Empty)
            throw new ArgumentException(
                "Reconciliation ID is required.");

        if (settlementBatchId == Guid.Empty)
            throw new ArgumentException(
                "Settlement batch ID is required.");

        if (expectedAmountMinor < 0)
            throw new ArgumentOutOfRangeException(
                nameof(expectedAmountMinor));

        if (reportedAmountMinor < 0)
            throw new ArgumentOutOfRangeException(
                nameof(reportedAmountMinor));

        ReconciliationId = reconciliationId;
        SettlementBatchId = settlementBatchId;
        ExpectedAmountMinor = expectedAmountMinor;
        ReportedAmountMinor = reportedAmountMinor;
        CurrencyCode = NormalizeCurrency(currencyCode);
        ExternalReference = externalReference?.Trim();

        DifferenceMinor =
            checked(reportedAmountMinor - expectedAmountMinor);

        Status = DifferenceMinor == 0
            ? ReconciliationStatus.Matched
            : ReconciliationStatus.Mismatched;
    }

    public Guid ReconciliationId { get; }

    public Guid SettlementBatchId { get; }

    public long ExpectedAmountMinor { get; }

    public long ReportedAmountMinor { get; }

    public long DifferenceMinor { get; }

    public string CurrencyCode { get; }

    public string? ExternalReference { get; }

    public ReconciliationStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; }
        = DateTime.UtcNow;

    public DateTime? ResolvedAtUtc { get; private set; }

    public void Resolve()
    {
        if (Status != ReconciliationStatus.Mismatched)
            throw new InvalidOperationException(
                "Only mismatched reconciliation records require resolution.");

        Status = ReconciliationStatus.Resolved;
        ResolvedAtUtc = DateTime.UtcNow;
    }

    private static string NormalizeCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Currency is required.");

        var result = value.Trim().ToUpperInvariant();

        if (result.Length != 3)
            throw new ArgumentException(
                "Currency must use ISO 4217.");

        return result;
    }
}
