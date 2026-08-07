namespace Treasury.Contracts;

public sealed record CreateTreasuryAccountRequest(
    string AccountId,
    string Name,
    string Currency);

public sealed record PostLedgerTransactionRequest(
    string TransactionId,
    string DebitAccountId,
    string CreditAccountId,
    decimal Amount);

public sealed record CreateReservationRequest(
    string ReservationId,
    string AccountId,
    decimal Amount);

public sealed record TreasuryAccountResponse(
    string AccountId,
    string Name,
    string Currency);

public sealed record TreasuryBalanceResponse(
    string AccountId,
    decimal Available,
    decimal Reserved,
    decimal Total);

public sealed record SettlementPositionResponse(
    string Partner,
    string Currency,
    decimal NetAmount);
