using Settlement.Domain.Fx;

namespace Settlement.Domain.Instructions;

public enum SettlementInstructionStatus
{
    Pending = 0,
    Settled = 1,
    Rejected = 2
}

public sealed class SettlementInstruction
{
    public Guid InstructionId { get; private set; }

    public Guid SourceAccountId { get; private set; }

    public Guid DestinationAccountId { get; private set; }

    public string SourceCurrency { get; private set; } = string.Empty;

    public string DestinationCurrency { get; private set; } = string.Empty;

    public long SourceAmountMinor { get; private set; }

    public long DestinationAmountMinor { get; private set; }

    public FxQuote? AppliedQuote { get; private set; }

    public SettlementInstructionStatus Status { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ExecutedAtUtc { get; private set; }

    public static SettlementInstruction Create(
        Guid sourceAccountId,
        Guid destinationAccountId,
        string sourceCurrency,
        string destinationCurrency,
        long sourceAmountMinor,
        FxQuote? quote)
    {
        if (sourceAccountId == Guid.Empty)
        {
            throw new ArgumentException("Source account ID is required.", nameof(sourceAccountId));
        }

        if (destinationAccountId == Guid.Empty)
        {
            throw new ArgumentException("Destination account ID is required.", nameof(destinationAccountId));
        }

        if (sourceAmountMinor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceAmountMinor), "Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(sourceCurrency) || string.IsNullOrWhiteSpace(destinationCurrency))
        {
            throw new ArgumentException("Currencies are required.");
        }

        var normalizedSourceCurrency = sourceCurrency.Trim().ToUpperInvariant();
        var normalizedDestinationCurrency = destinationCurrency.Trim().ToUpperInvariant();

        if (normalizedSourceCurrency != normalizedDestinationCurrency && quote is null)
        {
            throw new InvalidOperationException("An FX quote is required for cross-currency settlement.");
        }

        var destinationAmountMinor = normalizedSourceCurrency == normalizedDestinationCurrency
            ? sourceAmountMinor
            : quote!.Convert(sourceAmountMinor);

        return new SettlementInstruction
        {
            InstructionId = Guid.NewGuid(),
            SourceAccountId = sourceAccountId,
            DestinationAccountId = destinationAccountId,
            SourceCurrency = normalizedSourceCurrency,
            DestinationCurrency = normalizedDestinationCurrency,
            SourceAmountMinor = sourceAmountMinor,
            DestinationAmountMinor = destinationAmountMinor,
            AppliedQuote = quote,
            Status = SettlementInstructionStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarkSettled()
    {
        if (Status == SettlementInstructionStatus.Settled)
        {
            return;
        }

        if (Status == SettlementInstructionStatus.Rejected)
        {
            throw new InvalidOperationException("Rejected instructions cannot be settled.");
        }

        Status = SettlementInstructionStatus.Settled;
        ExecutedAtUtc = DateTime.UtcNow;
    }

    public void MarkRejected(string reason)
    {
        if (Status != SettlementInstructionStatus.Pending)
        {
            return;
        }

        Status = SettlementInstructionStatus.Rejected;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Rejected." : reason.Trim();
        ExecutedAtUtc = DateTime.UtcNow;
    }
}
