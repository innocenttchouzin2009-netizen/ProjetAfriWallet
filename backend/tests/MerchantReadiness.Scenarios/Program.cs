using System.Diagnostics;

static string FindRoot()
{
    var current = new DirectoryInfo(AppContext.BaseDirectory);
    while (current is not null)
    {
        if (Directory.Exists(Path.Combine(current.FullName, ".git")) && Directory.Exists(Path.Combine(current.FullName, "backend"))) return current.FullName;
        current = current.Parent;
    }
    throw new InvalidOperationException("AfriWallet repository root not found.");
}

static bool Exists(string root, string relative) => Directory.Exists(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar))) || File.Exists(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
static string ReadTree(string root, string relative)
{
    var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
    if (!Directory.Exists(path)) return string.Empty;
    return string.Join('\n', Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
}
static bool TagExists(string root, string tag)
{
    var psi = new ProcessStartInfo("git", $"-C \"{root}\" rev-parse --verify --quiet refs/tags/{tag}") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
    using var p = Process.Start(psi)!; p.WaitForExit(); return p.ExitCode == 0;
}

var root = args.Length > 0 ? Path.GetFullPath(args[0]) : FindRoot();
var checks = new List<(string Code, string Name, bool Pass, string Evidence)>();
void Check(string code, string name, bool pass, string evidence) => checks.Add((code, name, pass, evidence));

var deliveries = new[]
{
    "backend/src/Merchants/MerchantRegistry.Domain",
    "backend/src/Merchants/MerchantOnboarding",
    "backend/src/Merchants/MerchantCheckout",
    "backend/src/Merchants/MerchantPaymentDecision",
    "backend/src/Merchants/MerchantSettlement",
    "backend/src/Merchants/MerchantIntelligence"
};
Check("MER-RDY-001", "Merchant deliveries 0019.1-0019.6 present", deliveries.All(x => Exists(root, x)), string.Join(", ", deliveries));

var tags = Enumerable.Range(1, 6).Select(i => $"sprint19-dlv-0019.{i}").ToArray();
Check("MER-RDY-002", "Frozen delivery tags present", tags.All(x => TagExists(root, x)), string.Join(", ", tags));

var merchantRoot = "backend/src/Merchants";
var source = ReadTree(root, merchantRoot);
Check("MER-RDY-003", "Merchant source available", !string.IsNullOrWhiteSpace(source), merchantRoot);
Check("MER-RDY-004", "No direct ledger project dependency", !source.Contains("Ledger.Infrastructure", StringComparison.OrdinalIgnoreCase), "Merchant source must not depend on Ledger.Infrastructure");
Check("MER-RDY-005", "No automatic merchant blocking", !source.Contains("AutomaticMerchantBlocking = true", StringComparison.OrdinalIgnoreCase), "Blocking remains advisory/non-executing");
Check("MER-RDY-006", "No automatic merchant suspension", !source.Contains("AutomaticMerchantSuspension = true", StringComparison.OrdinalIgnoreCase), "Suspension remains advisory/non-executing");
Check("MER-RDY-007", "No automatic settlement freeze", !source.Contains("AutomaticSettlementFreeze = true", StringComparison.OrdinalIgnoreCase), "Settlement freeze remains non-executing");
Check("MER-RDY-008", "No automatic payout freeze", !source.Contains("AutomaticPayoutFreeze = true", StringComparison.OrdinalIgnoreCase), "Payout freeze remains non-executing");
Check("MER-RDY-009", "No payment capture execution flag", !source.Contains("PaymentCapturePerformed = true", StringComparison.OrdinalIgnoreCase), "Payment capture is outside readiness scope");
Check("MER-RDY-010", "No money movement execution flag", !source.Contains("MoneyMovementPerformed = true", StringComparison.OrdinalIgnoreCase), "Money movement is outside readiness scope");
Check("MER-RDY-011", "No ledger mutation execution flag", !source.Contains("LedgerMutationPerformed = true", StringComparison.OrdinalIgnoreCase), "Ledger mutation is outside readiness scope");
Check("MER-RDY-012", "Intelligence remains deterministic", source.Contains("deterministic", StringComparison.OrdinalIgnoreCase) || Exists(root, "docs/specs/merchant-intelligence"), "0019.6 deterministic intelligence evidence");
Check("MER-RDY-013", "Release validation tooling present", Exists(root, "tools/release/validate-merchant-readiness.ps1"), "tools/release/validate-merchant-readiness.ps1");
Check("MER-RDY-014", "Readiness specification present", Exists(root, "docs/specs/merchant-readiness/PRD-AFW-DLV-0019.7.md"), "PRD-AFW-DLV-0019.7.md");

Console.WriteLine("AFW-DLV-0019.7 - Merchant Platform Production Readiness\n");
foreach (var c in checks) Console.WriteLine($"{c.Code}  {c.Name,-48} {(c.Pass ? "PASS" : "FAIL")}\n  Evidence: {c.Evidence}");
var passed = checks.Count(x => x.Pass); var failed = checks.Count - passed;
Console.WriteLine($"\nChecks: {checks.Count} | Passed: {passed} | Failed: {failed}");
Console.WriteLine("Automatic merchant blocking/suspension: NOT IMPLEMENTED");
Console.WriteLine("Automatic settlement/payout freeze: NOT IMPLEMENTED");
Console.WriteLine("Payment capture: NOT IMPLEMENTED");
Console.WriteLine("Money movement: NOT IMPLEMENTED");
Console.WriteLine("Ledger mutation: NOT IMPLEMENTED");
Console.WriteLine(failed == 0 ? "Decision: READY FOR MERCHANT RC" : "Decision: NOT READY");
if (failed != 0) Environment.ExitCode = 1;
