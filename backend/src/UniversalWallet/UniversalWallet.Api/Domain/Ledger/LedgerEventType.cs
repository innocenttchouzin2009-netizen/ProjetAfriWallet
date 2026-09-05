namespace UniversalWallet.Api.Domain.Ledger;

public enum LedgerEventType
{
	TransactionPosted,
	TransactionRejected,
	TransactionReversed,
	LedgerBalanced,
	LedgerMismatchDetected
}
