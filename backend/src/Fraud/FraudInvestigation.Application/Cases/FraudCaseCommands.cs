using AfriWallet.Fraud.Investigation.Domain.Cases;
using AfriWallet.Fraud.Investigation.Domain.Responses;

namespace AfriWallet.Fraud.Investigation.Application.Cases;

public sealed record CreateFraudCaseCommand(string Awid, Guid TransactionId, string Title, FraudCasePriority Priority, string Actor);
public sealed record AssignFraudCaseCommand(Guid CaseId, string AnalystId, string Actor);
public sealed record AddFraudCaseNoteCommand(Guid CaseId, string Content, string Actor);
public sealed record EscalateFraudCaseCommand(Guid CaseId, FraudCasePriority Priority, string Actor);
public sealed record AddFraudResponseCommand(Guid CaseId, FraudResponseType ResponseType, string Reason, string Actor);
public sealed record ResolveFraudCaseCommand(Guid CaseId, FraudCaseResolution Resolution, string Actor);