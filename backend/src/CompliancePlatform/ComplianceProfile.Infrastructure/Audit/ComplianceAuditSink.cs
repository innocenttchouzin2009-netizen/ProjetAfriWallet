using AfriWallet.CompliancePlatform.ComplianceProfile.Application.Interfaces;

namespace AfriWallet.CompliancePlatform.ComplianceProfile.Infrastructure.Audit;

public sealed class ComplianceAuditSink : IComplianceAuditSink
{
    public Task WriteAsync(string profileId, string action, string detail, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entry = $"{DateTimeOffset.UtcNow:O}|{profileId}|{action}|{detail}";
        File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "compliance-profile-audit.log"), entry + Environment.NewLine);

        return Task.CompletedTask;
    }
}
