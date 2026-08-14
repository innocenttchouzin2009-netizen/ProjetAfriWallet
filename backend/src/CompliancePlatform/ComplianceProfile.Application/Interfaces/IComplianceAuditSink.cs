namespace AfriWallet.CompliancePlatform.ComplianceProfile.Application.Interfaces;

public interface IComplianceAuditSink
{
    Task WriteAsync(string profileId, string action, string detail, CancellationToken cancellationToken);
}
