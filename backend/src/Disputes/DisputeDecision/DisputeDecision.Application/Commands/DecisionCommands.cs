namespace AfriWallet.Disputes.Decision.Application.Commands;

public sealed record EvaluateDisputeDecisionCommand(Guid InvestigationId, string Actor);
public sealed record ApproveDisputeDecisionCommand(Guid DecisionId, string Approver, string Note, string Actor);
public sealed record ReevaluateDisputeDecisionCommand(Guid InvestigationId, string Actor, string Reason);
