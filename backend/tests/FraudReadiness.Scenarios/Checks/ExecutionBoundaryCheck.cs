using AfriWallet.Fraud.Readiness.Models;

namespace AfriWallet.Fraud.Readiness.Checks;

public sealed class ExecutionBoundaryCheck : IFraudReadinessCheck
{
    public string Code => "FRD-RDY-004";
    public Task<ReadinessCheck> ExecuteAsync(string root, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var forbidden = new[] { "ExecutePaymentAsync(", "SuspendAccountAsync(", "FreezeWalletAsync(", "RevokeDeviceAsync(", "CancelBankTransferAsync(" };
        var findings = RepositoryCheckUtilities.EnumerateTextFiles(RepositoryCheckUtilities.Resolve(root, "backend", "src", "Fraud")).SelectMany(file => forbidden.Where(token => File.ReadAllText(file).Contains(token, StringComparison.Ordinal)).Select(token => $"{Path.GetFileName(file)}:{token}")).ToArray();
        return Task.FromResult(RepositoryCheckUtilities.Result(Code, "Fraud execution boundary", findings.Length == 0, findings.Length == 0 ? "No direct enforcement API detected" : string.Join(", ", findings)));
    }
}