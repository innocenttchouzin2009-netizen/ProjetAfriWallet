using TreasuryReleaseCandidate.Manifest;
using TreasuryReleaseCandidate.Reports;
using TreasuryReleaseCandidate.Validation;

var repositoryRoot = Directory.GetCurrentDirectory();
var releaseRoot = Path.Combine(repositoryRoot, "release", "financial-platform", "v1.3.0-rc1");

var validator = new TreasuryRcValidator(repositoryRoot);
var summary = validator.Run();

foreach (var check in summary.Checks)
{
	Console.WriteLine($"{check.Name,-48} {(check.Passed ? "PASS" : "FAIL")}");
}

Console.WriteLine();
Console.WriteLine($"Checks: {summary.Checks.Count}");
Console.WriteLine($"Passed: {summary.Passed}");
Console.WriteLine($"Failed: {summary.Failed}");
Console.WriteLine($"Skipped: {summary.Skipped}");
Console.WriteLine();
Console.WriteLine(summary.Success ? "Decision: READY FOR TREASURY RC" : "Decision: NOT READY");

if (!summary.Success)
{
	Environment.ExitCode = 1;
	return;
}

Directory.CreateDirectory(releaseRoot);

var reportWriter = new ReleaseReportWriter();
reportWriter.Write(
	releaseRoot,
	"v1.3.0-rc1",
	"AFW-DLV-0013.8",
	"READY FOR TREASURY RC",
	summary);

WriteReleaseNotes(releaseRoot);
CopyReleaseAssets(repositoryRoot, releaseRoot);

var checksumWriter = new ChecksumWriter();
checksumWriter.Write(releaseRoot, "checksums.sha256");

var manifestWriter = new ReleaseManifestWriter();
manifestWriter.Write(
	releaseRoot,
	"AfriWallet Treasury Platform Release Candidate Package",
	"v1.3.0-rc1",
	"AFW-DLV-0013.8",
	"READY FOR TREASURY RC");

static void WriteReleaseNotes(string releaseRoot)
{
	var releaseNotes = """
# Treasury Platform Release Candidate v1.3.0-rc1

This package closes Sprint 13 and aggregates deliveries AFW-DLV-0013.1 to AFW-DLV-0013.7.

- Delivery stream: AFW-DLV-0013.8
- Decision: READY FOR TREASURY RC
- Scope: consolidation and release evidence packaging only
""";

	var changelog = """
# Changelog

## v1.3.0-rc1
- Consolidated treasury ledger, liquidity, settlement, reconciliation, accounting, production readiness, and disaster recovery evidence.
- Generated RC validation report, manifest, and checksums.
""";

	File.WriteAllText(Path.Combine(releaseRoot, "release-notes.md"), releaseNotes);
	File.WriteAllText(Path.Combine(releaseRoot, "changelog.md"), changelog);
}

static void CopyReleaseAssets(string repositoryRoot, string releaseRoot)
{
	var openApiDestination = Path.Combine(releaseRoot, "openapi");
	var adrDestination = Path.Combine(releaseRoot, "adr");
	var runbookDestination = Path.Combine(releaseRoot, "runbooks");
	var dashboardDestination = Path.Combine(releaseRoot, "dashboards");
	var configurationDestination = Path.Combine(releaseRoot, "configuration");
	var artifactsDestination = Path.Combine(releaseRoot, "artifacts");
	var rollbackDestination = Path.Combine(releaseRoot, "rollback");
	var drDestination = Path.Combine(releaseRoot, "dr");

	Directory.CreateDirectory(openApiDestination);
	Directory.CreateDirectory(adrDestination);
	Directory.CreateDirectory(runbookDestination);
	Directory.CreateDirectory(dashboardDestination);
	Directory.CreateDirectory(configurationDestination);
	Directory.CreateDirectory(artifactsDestination);
	Directory.CreateDirectory(rollbackDestination);
	Directory.CreateDirectory(drDestination);

	Copy(repositoryRoot, "docs/specs/treasury-ledger/openapi.yaml", Path.Combine(openApiDestination, "treasury-ledger.yaml"));
	Copy(repositoryRoot, "docs/specs/liquidity-engine/openapi.yaml", Path.Combine(openApiDestination, "liquidity-engine.yaml"));
	Copy(repositoryRoot, "docs/specs/multi-currency-settlement/openapi.yaml", Path.Combine(openApiDestination, "multi-currency-settlement.yaml"));
	Copy(repositoryRoot, "docs/specs/reconciliation-platform/openapi.yaml", Path.Combine(openApiDestination, "reconciliation-platform.yaml"));
	Copy(repositoryRoot, "docs/specs/accounting-general-ledger/openapi.yaml", Path.Combine(openApiDestination, "accounting-general-ledger.yaml"));

	Copy(repositoryRoot, "docs/specs/treasury-ledger/ADR-0212-treasury-ledger-architecture.md", Path.Combine(adrDestination, "ADR-0212-treasury-ledger-architecture.md"));
	Copy(repositoryRoot, "docs/specs/treasury-ledger/ADR-0213-double-entry-treasury-journal.md", Path.Combine(adrDestination, "ADR-0213-double-entry-treasury-journal.md"));
	Copy(repositoryRoot, "docs/specs/liquidity-engine/ADR-0214-liquidity-engine.md", Path.Combine(adrDestination, "ADR-0214-liquidity-engine.md"));
	Copy(repositoryRoot, "docs/specs/liquidity-engine/ADR-0215-liquidity-rebalancing.md", Path.Combine(adrDestination, "ADR-0215-liquidity-rebalancing.md"));
	Copy(repositoryRoot, "docs/specs/multi-currency-settlement/ADR-0216-settlement-orchestration.md", Path.Combine(adrDestination, "ADR-0216-settlement-orchestration.md"));
	Copy(repositoryRoot, "docs/specs/multi-currency-settlement/ADR-0217-fx-quote-policy.md", Path.Combine(adrDestination, "ADR-0217-fx-quote-policy.md"));
	Copy(repositoryRoot, "docs/specs/reconciliation-platform/ADR-0218-reconciliation-architecture.md", Path.Combine(adrDestination, "ADR-0218-reconciliation-architecture.md"));
	Copy(repositoryRoot, "docs/specs/reconciliation-platform/ADR-0219-matching-and-exception-strategy.md", Path.Combine(adrDestination, "ADR-0219-matching-and-exception-strategy.md"));
	Copy(repositoryRoot, "docs/specs/treasury-production-readiness/ADR-0222-treasury-production-readiness.md", Path.Combine(adrDestination, "ADR-0222-treasury-production-readiness.md"));
	Copy(repositoryRoot, "docs/specs/treasury-production-readiness/ADR-0223-financial-core-validation-strategy.md", Path.Combine(adrDestination, "ADR-0223-financial-core-validation-strategy.md"));
	Copy(repositoryRoot, "docs/specs/treasury-disaster-recovery/ADR-0224-treasury-disaster-recovery-architecture.md", Path.Combine(adrDestination, "ADR-0224-treasury-disaster-recovery-architecture.md"));
	Copy(repositoryRoot, "docs/specs/treasury-disaster-recovery/ADR-0225-rpo-rto-and-failover-strategy.md", Path.Combine(adrDestination, "ADR-0225-rpo-rto-and-failover-strategy.md"));
	Copy(repositoryRoot, "docs/specs/treasury-release-candidate/ADR-0226-treasury-rc-consolidation-policy.md", Path.Combine(adrDestination, "ADR-0226-treasury-rc-consolidation-policy.md"));
	Copy(repositoryRoot, "docs/specs/treasury-release-candidate/ADR-0227-treasury-rc-evidence-packaging.md", Path.Combine(adrDestination, "ADR-0227-treasury-rc-evidence-packaging.md"));

	Copy(repositoryRoot, "docs/specs/treasury-production-readiness/operations-runbook.md", Path.Combine(runbookDestination, "production-operations-runbook.md"));
	Copy(repositoryRoot, "docs/specs/treasury-disaster-recovery/failover-runbook.md", Path.Combine(runbookDestination, "disaster-recovery-failover-runbook.md"));
	Copy(repositoryRoot, "docs/specs/treasury-disaster-recovery/restore-runbook.md", Path.Combine(runbookDestination, "disaster-recovery-restore-runbook.md"));

	File.WriteAllText(
		Path.Combine(dashboardDestination, "treasury-platform-overview.md"),
		"# Treasury Platform Dashboard Overview\n\n- Treasury ledger posting flow\n- Liquidity positions and rebalance indicators\n- Settlement execution success rate\n- Reconciliation exceptions\n- Disaster recovery RPO/RTO status\n");

	File.WriteAllText(
		Path.Combine(configurationDestination, "release-settings.json"),
		"""
{
  "stream": "AFW-DLV-0013.8",
  "version": "v1.3.0-rc1",
  "configuration": "Release",
  "decision": "READY FOR TREASURY RC"
}
""");

	Copy(repositoryRoot, "docs/specs/treasury-release-candidate/PRD-AFW-DLV-0013.8.md", Path.Combine(artifactsDestination, "PRD-AFW-DLV-0013.8.md"));
	Copy(repositoryRoot, "docs/specs/treasury-release-candidate/qa-checklist.md", Path.Combine(artifactsDestination, "qa-checklist.md"));
	Copy(repositoryRoot, "docs/specs/treasury-release-candidate/release-guide.md", Path.Combine(artifactsDestination, "release-guide.md"));
	Copy(repositoryRoot, "docs/specs/treasury-release-candidate/validation-report.md", Path.Combine(artifactsDestination, "validation-report-template.md"));

	Copy(repositoryRoot, "docs/specs/treasury-release-candidate/rollback-plan.md", Path.Combine(rollbackDestination, "rollback-plan.md"));
	Copy(repositoryRoot, "release/financial-platform/v1.3.0/dr/validation-report.json", Path.Combine(drDestination, "validation-report.json"));
	Copy(repositoryRoot, "release/financial-platform/v1.3.0/dr/validation-report.md", Path.Combine(drDestination, "validation-report.md"));
	Copy(repositoryRoot, "release/financial-platform/v1.3.0/dr/checksums.sha256", Path.Combine(drDestination, "checksums.sha256"));
}

static void Copy(string repositoryRoot, string relativeSourcePath, string destinationPath)
{
	var sourcePath = Path.Combine(repositoryRoot, relativeSourcePath.Replace('/', Path.DirectorySeparatorChar));

	if (!File.Exists(sourcePath))
	{
		throw new FileNotFoundException($"Required release asset is missing: {relativeSourcePath}", sourcePath);
	}

	var destinationDirectory = Path.GetDirectoryName(destinationPath);
	if (!string.IsNullOrWhiteSpace(destinationDirectory))
	{
		Directory.CreateDirectory(destinationDirectory);
	}

	File.Copy(sourcePath, destinationPath, overwrite: true);
}
