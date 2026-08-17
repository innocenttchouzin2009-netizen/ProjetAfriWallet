namespace AfriWallet.Fraud.Intelligence.Application.Abstractions;

public interface IFraudIntelligenceClock { DateTimeOffset UtcNow { get; } }