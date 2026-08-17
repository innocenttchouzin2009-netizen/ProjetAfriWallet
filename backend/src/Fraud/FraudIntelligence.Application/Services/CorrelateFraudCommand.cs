namespace AfriWallet.Fraud.Intelligence.Application.Services;

public sealed record CorrelateFraudCommand(string Awid, string Actor);