namespace TreasuryDisasterRecovery.Integrity;

public sealed record FinancialIntegritySnapshot(
    long TreasuryDebitMinor,
    long TreasuryCreditMinor,
    long AccountingDebitMinor,
    long AccountingCreditMinor,
    int TreasuryTransactions,
    int AccountingJournals,
    int ActiveReservations,
    int CompletedSettlements,
    int ReconciliationExceptions,
    DateTime CapturedAtUtc)
{
    public bool IsDoubleEntryBalanced =>
        TreasuryDebitMinor == TreasuryCreditMinor &&
        AccountingDebitMinor == AccountingCreditMinor;
}