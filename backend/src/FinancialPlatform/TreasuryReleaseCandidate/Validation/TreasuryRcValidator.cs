namespace TreasuryReleaseCandidate.Validation;

public sealed class TreasuryRcValidator
{
    private readonly string _repositoryRoot;

    public TreasuryRcValidator(string repositoryRoot)
    {
        _repositoryRoot = repositoryRoot;
    }

    public ReleaseValidationSummary Run()
    {
        var checks = new List<ReleaseCheck>
        {
            ValidateFile("Treasury production validation report", "release/financial-platform/v1.3.0/validation-report.json"),
            ValidateReportDecision("Treasury production decision", "release/financial-platform/v1.3.0/validation-report.json", "READY FOR TREASURY RC"),
            ValidateFile("Treasury disaster recovery validation report", "release/financial-platform/v1.3.0/dr/validation-report.json"),
            ValidateReportDecision("Treasury disaster recovery decision", "release/financial-platform/v1.3.0/dr/validation-report.json", "READY FOR TREASURY RC"),
            ValidateFile("Treasury ledger OpenAPI", "docs/specs/treasury-ledger/openapi.yaml"),
            ValidateFile("Liquidity OpenAPI", "docs/specs/liquidity-engine/openapi.yaml"),
            ValidateFile("Settlement OpenAPI", "docs/specs/multi-currency-settlement/openapi.yaml"),
            ValidateFile("Reconciliation OpenAPI", "docs/specs/reconciliation-platform/openapi.yaml"),
            ValidateFile("Accounting OpenAPI", "docs/specs/accounting-general-ledger/openapi.yaml"),
            ValidateFile("Treasury RC PRD", "docs/specs/treasury-release-candidate/PRD-AFW-DLV-0013.8.md"),
            ValidateFile("Treasury RC release guide", "docs/specs/treasury-release-candidate/release-guide.md"),
            ValidateFile("Treasury RC rollback plan", "docs/specs/treasury-release-candidate/rollback-plan.md"),
            ValidateFile("Treasury RC QA checklist", "docs/specs/treasury-release-candidate/qa-checklist.md"),
            ValidateFile("Treasury RC validation template", "docs/specs/treasury-release-candidate/validation-report.md")
        };

        return new ReleaseValidationSummary(checks);
    }

    private ReleaseCheck ValidateFile(string name, string relativePath)
    {
        var fullPath = Resolve(relativePath);

        if (!File.Exists(fullPath))
        {
            return new ReleaseCheck(name, false, $"Missing required file: {relativePath}");
        }

        return new ReleaseCheck(name, true, "ok");
    }

    private ReleaseCheck ValidateReportDecision(string name, string relativePath, string expectedDecision)
    {
        var fullPath = Resolve(relativePath);

        if (!File.Exists(fullPath))
        {
            return new ReleaseCheck(name, false, $"Missing required file: {relativePath}");
        }

        var content = File.ReadAllText(fullPath);

        if (!content.Contains(expectedDecision, StringComparison.OrdinalIgnoreCase))
        {
            return new ReleaseCheck(name, false, $"Expected decision not found: {expectedDecision}");
        }

        return new ReleaseCheck(name, true, "ok");
    }

    private string Resolve(string relativePath)
    {
        return Path.Combine(_repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
