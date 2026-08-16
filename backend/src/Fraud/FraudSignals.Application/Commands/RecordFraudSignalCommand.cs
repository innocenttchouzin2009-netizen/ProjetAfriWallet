using AfriWallet.Fraud.Signals.Domain.Enums;
namespace AfriWallet.Fraud.Signals.Application.Commands;
public sealed record RecordFraudSignalCommand(string EventId,FraudSignalSource Source,FraudSignalType Type,FraudSignalSeverity Severity,string SubjectType,string SubjectId,DateTimeOffset OccurredAt,IReadOnlyDictionary<string,string>? Attributes=null);