namespace AfriWallet.Fraud.Investigation.Domain.Responses;

public sealed record FraudResponseRecommendation(FraudResponseType Type, string Reason, DateTimeOffset RecommendedAtUtc);