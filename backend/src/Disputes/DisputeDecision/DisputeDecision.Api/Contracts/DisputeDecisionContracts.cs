namespace AfriWallet.Disputes.Decision.Api.Contracts;

public sealed record EvaluateDecisionRequest(Guid InvestigationId);
public sealed record ApproveDecisionRequest(string Approver, string Note);
public sealed record ReevaluateDecisionRequest(Guid InvestigationId, string Reason);
