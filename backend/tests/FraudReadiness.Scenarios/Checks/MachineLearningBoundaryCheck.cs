using AfriWallet.Fraud.Readiness.Models;

namespace AfriWallet.Fraud.Readiness.Checks;

public sealed class MachineLearningBoundaryCheck : IFraudReadinessCheck
{
    public string Code => "FRD-RDY-006";
    public Task<ReadinessCheck> ExecuteAsync(string root, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var prohibited = new[] { "Microsoft.ML", "TensorFlow", "ONNXRuntime", "ML.NET", "Predict(", "PredictionEngine" };
        var findings = RepositoryCheckUtilities.EnumerateTextFiles(RepositoryCheckUtilities.Resolve(root, "backend", "src", "Fraud")).SelectMany(file => prohibited.Where(token => File.ReadAllText(file).Contains(token, StringComparison.OrdinalIgnoreCase)).Select(token => $"{Path.GetFileName(file)}:{token}")).ToArray();
        return Task.FromResult(RepositoryCheckUtilities.Result(Code, "Opaque ML boundary", findings.Length == 0, findings.Length == 0 ? "Fraud platform remains deterministic" : string.Join(", ", findings)));
    }
}