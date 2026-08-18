namespace AfriWallet.Disputes.Readiness.Models;

public sealed record ReadinessCheck(string Code, string Name, ReadinessStatus Status, string Evidence);
