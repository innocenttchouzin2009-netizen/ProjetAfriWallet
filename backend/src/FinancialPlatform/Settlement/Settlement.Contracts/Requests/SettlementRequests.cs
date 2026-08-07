namespace Settlement.Contracts.Requests;

public sealed record CreateSettlementInstructionRequest(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    string SourceCurrency,
    string DestinationCurrency,
    long SourceAmountMinor);

public sealed record CreateSettlementBatchRequest(IReadOnlyCollection<Guid> InstructionIds);
