namespace AfriWallet.Fraud.Signals.Application.Abstractions;
public interface ISystemClock { DateTimeOffset UtcNow{get;} }