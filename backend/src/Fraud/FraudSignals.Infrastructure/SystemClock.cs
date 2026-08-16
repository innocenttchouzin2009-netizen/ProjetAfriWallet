using AfriWallet.Fraud.Signals.Application.Abstractions;
namespace AfriWallet.Fraud.Signals.Infrastructure;
public sealed class SystemClock:ISystemClock { public DateTimeOffset UtcNow=>DateTimeOffset.UtcNow; }