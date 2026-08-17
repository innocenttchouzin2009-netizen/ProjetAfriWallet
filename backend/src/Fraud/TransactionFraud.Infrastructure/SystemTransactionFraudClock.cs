using AfriWallet.Fraud.TransactionFraud.Application.Abstractions;

namespace AfriWallet.Fraud.TransactionFraud.Infrastructure;

public sealed class SystemTransactionFraudClock : ITransactionFraudClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
