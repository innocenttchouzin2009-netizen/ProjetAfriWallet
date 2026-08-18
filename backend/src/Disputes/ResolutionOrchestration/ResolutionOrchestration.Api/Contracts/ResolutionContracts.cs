namespace AfriWallet.Disputes.Resolution.Api.Contracts;

public sealed record CreateResolutionRequest(Guid DecisionId, string IdempotencyKey);
