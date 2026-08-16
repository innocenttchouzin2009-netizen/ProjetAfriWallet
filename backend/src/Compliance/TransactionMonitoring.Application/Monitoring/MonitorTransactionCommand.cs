using AfriWallet.Compliance.TransactionMonitoring.Domain.Transactions;

namespace AfriWallet.Compliance.TransactionMonitoring.Application.Monitoring;

public sealed record MonitorTransactionCommand(
    MonitoredTransaction Transaction,
    string Actor);