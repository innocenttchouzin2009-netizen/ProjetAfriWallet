namespace AfriWallet.Disputes.Resolution.Application.Commands;

public sealed record CreateResolutionCommand(Guid DecisionId, string IdempotencyKey, string Actor);
public sealed record DispatchResolutionCommand(Guid ResolutionId, string Actor);
public sealed record RetryResolutionCommand(Guid ResolutionId, string Actor);
public sealed record CompensateResolutionCommand(Guid ResolutionId, string Actor);
public sealed record ResolveResolutionCommand(Guid ResolutionId, string Actor);
