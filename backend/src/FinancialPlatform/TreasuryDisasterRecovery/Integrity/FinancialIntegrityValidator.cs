namespace TreasuryDisasterRecovery.Integrity;

public sealed class FinancialIntegrityValidator
{
    public IntegrityValidationResult Validate(FinancialIntegritySnapshot before, FinancialIntegritySnapshot after)
    {
        var violations = new List<string>();

        if (!after.IsDoubleEntryBalanced)
            violations.Add("DOUBLE_ENTRY_NOT_BALANCED");

        if (before.TreasuryDebitMinor != after.TreasuryDebitMinor)
            violations.Add("TREASURY_DEBIT_CHANGED");

        if (before.TreasuryCreditMinor != after.TreasuryCreditMinor)
            violations.Add("TREASURY_CREDIT_CHANGED");

        if (before.AccountingDebitMinor != after.AccountingDebitMinor)
            violations.Add("ACCOUNTING_DEBIT_CHANGED");

        if (before.AccountingCreditMinor != after.AccountingCreditMinor)
            violations.Add("ACCOUNTING_CREDIT_CHANGED");

        if (before.TreasuryTransactions != after.TreasuryTransactions)
            violations.Add("TREASURY_TRANSACTION_COUNT_CHANGED");

        if (before.AccountingJournals != after.AccountingJournals)
            violations.Add("ACCOUNTING_JOURNAL_COUNT_CHANGED");

        return new IntegrityValidationResult(violations.Count == 0, violations);
    }
}

public sealed record IntegrityValidationResult(bool Success, IReadOnlyCollection<string> Violations);