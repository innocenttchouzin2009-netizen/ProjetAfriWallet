namespace AfriWallet.Fraud.TransactionFraud.Application.Abstractions;

public interface ITransactionFraudClock
{
    DateTimeOffset UtcNow { get; }
}
