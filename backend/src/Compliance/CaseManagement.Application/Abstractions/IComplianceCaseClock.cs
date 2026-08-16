namespace AfriWallet.Compliance.CaseManagement.Application.Abstractions;

public interface IComplianceCaseClock
{
    DateTimeOffset UtcNow { get; }
}