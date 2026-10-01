namespace AfriWallet.TransactionRead.Application.Contracts;

public enum TransactionReadStatus
{
    Pending,
    Completed,
    Failed,
    Cancelled,
    Reversed
}
