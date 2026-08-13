namespace MerchantSettlement.Domain.Instructions;

public sealed class SettlementInstruction
{
    public SettlementInstruction(
        Guid instructionId,
        Guid paymentId,
        string merchantId,
        string currencyCode,
        long grossAmountMinor,
        long feeMinor,
        long netAmountMinor,
        string? payoutReference = null)
    {
        if (instructionId == Guid.Empty)
            throw new ArgumentException("Instruction ID is required.");

        if (paymentId == Guid.Empty)
            throw new ArgumentException("Payment ID is required.");

        if (string.IsNullOrWhiteSpace(merchantId))
            throw new ArgumentException("Merchant ID is required.");

        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Currency is required.");

        if (grossAmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(grossAmountMinor));

        InstructionId = instructionId;
        PaymentId = paymentId;
        MerchantId = merchantId.Trim();
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        GrossAmountMinor = grossAmountMinor;
        FeeMinor = feeMinor;
        NetAmountMinor = netAmountMinor;
        PayoutReference = payoutReference;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid InstructionId { get; }

    public Guid PaymentId { get; }

    public string MerchantId { get; }

    public string CurrencyCode { get; }

    public long GrossAmountMinor { get; }

    public long FeeMinor { get; }

    public long NetAmountMinor { get; }

    public string? PayoutReference { get; private set; }

    public SettlementInstructionStatus Status { get; private set; }
        = SettlementInstructionStatus.Created;

    public DateTime CreatedAtUtc { get; }

    public DateTime? SettledAtUtc { get; private set; }

    public void Approve()
    {
        if (Status != SettlementInstructionStatus.Created)
            throw new InvalidOperationException("Only created instructions can be approved.");

        Status = SettlementInstructionStatus.Approved;
    }

    public void Settle(string payoutReference)
    {
        if (Status != SettlementInstructionStatus.Approved)
            throw new InvalidOperationException("Only approved instructions can settle.");

        if (string.IsNullOrWhiteSpace(payoutReference))
            throw new ArgumentException("Payout reference is required.");

        PayoutReference = payoutReference;
        Status = SettlementInstructionStatus.Settled;
        SettledAtUtc = DateTime.UtcNow;
    }

    public void Fail(string reason)
    {
        if (Status == SettlementInstructionStatus.Settled)
            throw new InvalidOperationException("Settled instruction cannot fail.");

        Status = SettlementInstructionStatus.Failed;
    }
}

public enum SettlementInstructionStatus
{
    Created,
    Approved,
    Settled,
    Failed
}
