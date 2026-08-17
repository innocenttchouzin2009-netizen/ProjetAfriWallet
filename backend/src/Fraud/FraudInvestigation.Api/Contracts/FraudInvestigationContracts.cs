using AfriWallet.Fraud.Investigation.Domain.Cases;
using AfriWallet.Fraud.Investigation.Domain.Responses;

namespace AfriWallet.Fraud.Investigation.Api.Contracts;

public sealed record CreateFraudCaseRequest(string Awid, Guid TransactionId, string Title, FraudCasePriority Priority);
public sealed record AssignFraudCaseRequest(string AnalystId);
public sealed record AddFraudNoteRequest(string Content);
public sealed record EscalateFraudCaseRequest(FraudCasePriority Priority);
public sealed record AddFraudResponseRequest(FraudResponseType ResponseType, string Reason);
public sealed record ResolveFraudCaseRequest(FraudCaseResolution Resolution);