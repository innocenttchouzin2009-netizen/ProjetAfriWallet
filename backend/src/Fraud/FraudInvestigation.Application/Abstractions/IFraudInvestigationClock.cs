namespace AfriWallet.Fraud.Investigation.Application.Abstractions;

public interface IFraudInvestigationClock { DateTimeOffset UtcNow { get; } }