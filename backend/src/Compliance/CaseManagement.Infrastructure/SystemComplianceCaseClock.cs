using AfriWallet.Compliance.CaseManagement.Application.Abstractions;
namespace AfriWallet.Compliance.CaseManagement.Infrastructure;
public sealed class SystemComplianceCaseClock : IComplianceCaseClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }