namespace Liquidity.Domain.Rebalancing;

public sealed record LiquidityTransfer(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    long AmountMinor,
    string CurrencyCode);

public sealed record LiquidityRebalance(
    string CurrencyCode,
    DateTime ProposedAtUtc,
    IReadOnlyCollection<LiquidityTransfer> Transfers,
    string Rationale);
