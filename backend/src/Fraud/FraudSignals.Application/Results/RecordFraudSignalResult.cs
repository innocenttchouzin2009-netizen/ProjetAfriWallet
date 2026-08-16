namespace AfriWallet.Fraud.Signals.Application.Results;
public sealed record RecordFraudSignalResult(Guid SignalId,string EventId,bool Duplicate);