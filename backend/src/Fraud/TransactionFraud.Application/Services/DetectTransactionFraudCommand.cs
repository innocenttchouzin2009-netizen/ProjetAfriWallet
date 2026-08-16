using AfriWallet.Fraud.TransactionFraud.Domain.Transactions;

namespace AfriWallet.Fraud.TransactionFraud.Application.Services;

public sealed record DetectTransactionFraudCommand(
    FraudTransaction Transaction,
    string Actor);
