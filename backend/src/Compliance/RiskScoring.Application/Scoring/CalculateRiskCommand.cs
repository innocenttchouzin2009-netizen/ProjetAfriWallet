namespace AfriWallet.Compliance.RiskScoring.Application.Scoring;

public sealed record CalculateRiskCommand(string Awid, string Actor);