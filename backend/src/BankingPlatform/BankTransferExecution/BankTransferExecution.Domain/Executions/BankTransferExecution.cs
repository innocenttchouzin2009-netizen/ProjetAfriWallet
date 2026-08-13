namespace AfriWallet.BankingPlatform.BankTransferExecution.Domain.Executions;

public sealed class BankTransferExecution
{
    public BankTransferExecution(
        Guid executionId,
        Guid transferIntentId,
        Guid routingDecisionId,
        string providerCode,
        string railCode,
        long amountMinor,
        string currencyCode,
        string idempotencyKey)
    {
        if (executionId == Guid.Empty)
            throw new ArgumentException("Execution ID is required.");

        if (transferIntentId == Guid.Empty)
            throw new ArgumentException("Transfer intent ID is required.");

        if (routingDecisionId == Guid.Empty)
            throw new ArgumentException("Routing decision ID is required.");

        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor));

        ExecutionId = executionId;
        TransferIntentId = transferIntentId;
        RoutingDecisionId = routingDecisionId;
        ProviderCode = Require(providerCode);
        RailCode = Require(railCode);
        AmountMinor = amountMinor;
        CurrencyCode = NormalizeCurrency(currencyCode);
        IdempotencyKey = Require(idempotencyKey);
    }

    public Guid ExecutionId { get; }

    public Guid TransferIntentId { get; }

    public Guid RoutingDecisionId { get; }

    public string ProviderCode { get; }

    public string RailCode { get; }

    public long AmountMinor { get; }

    public string CurrencyCode { get; }

    public string IdempotencyKey { get; }

    public string? ProviderReference { get; private set; }

    public BankTransferExecutionStatus Status { get; private set; }
        = BankTransferExecutionStatus.Created;

    public DateTime CreatedAtUtc { get; }
        = DateTime.UtcNow;

    public DateTime? SubmittedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public string? FailureCode { get; private set; }

    public void Start()
    {
        if (Status != BankTransferExecutionStatus.Created)
            throw new InvalidOperationException(
                "Only created executions may start.");

        Status = BankTransferExecutionStatus.Processing;
    }

    public void MarkSubmitted(string providerReference)
    {
        if (Status != BankTransferExecutionStatus.Processing)
            throw new InvalidOperationException(
                "Execution is not processing.");

        ProviderReference = Require(providerReference);
        Status = BankTransferExecutionStatus.Submitted;
        SubmittedAtUtc = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != BankTransferExecutionStatus.Submitted)
            throw new InvalidOperationException(
                "Only submitted executions may complete.");

        Status = BankTransferExecutionStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Fail(string failureCode)
    {
        if (Status == BankTransferExecutionStatus.Completed)
            throw new InvalidOperationException(
                "Completed execution is immutable.");

        FailureCode = Require(failureCode);
        Status = BankTransferExecutionStatus.Failed;
    }

    private static string Require(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.");

        return value.Trim();
    }

    private static string NormalizeCurrency(string value)
    {
        var normalized = Require(value).ToUpperInvariant();

        if (normalized.Length != 3)
            throw new ArgumentException(
                "Currency must use ISO 4217.");

        return normalized;
    }
}

public enum BankTransferExecutionStatus
{
    Created,
    Processing,
    Submitted,
    Completed,
    Failed
}
