namespace AfriWallet.PaymentPlatform.ProductionReadiness.Validation;

public sealed class PaymentPlatformReadinessValidator
{
    private readonly string _repositoryRoot;
    private readonly string _evidenceDirectory;
    private readonly string _releaseDirectory;

    public PaymentPlatformReadinessValidator(
        string repositoryRoot,
        string evidenceDirectory)
    {
        _repositoryRoot = Path.GetFullPath(repositoryRoot);
        _evidenceDirectory = Path.GetFullPath(evidenceDirectory);
        _releaseDirectory = Path.Combine(
            _repositoryRoot,
            "release",
            "payment-platform",
            "v1.4.0");
    }

    public ReadinessSummary Run()
    {
        var summary = new ReadinessSummary();

        AddScenarioCheck(
            summary,
            "Payment Intent",
            "scenario-payment-intent.log",
            "All AFW-DLV-0014.1 payment intent scenarios passed.");
        AddScenarioCheck(
            summary,
            "Payment Routing",
            "scenario-payment-routing.log",
            "All AFW-DLV-0014.2 payment routing scenarios passed.");
        AddScenarioCheck(
            summary,
            "Merchant Acquiring",
            "scenario-merchant-acquiring.log",
            "All AFW-DLV-0014.3 merchant acquiring scenarios passed.");
        AddScenarioCheck(
            summary,
            "Merchant Settlement",
            "scenario-merchant-settlement.log",
            "All AFW-DLV-0014.4 merchant settlement scenarios passed.");
        AddScenarioCheck(
            summary,
            "Mobile Money Gateway",
            "scenario-mobile-money.log",
            "All AFW-DLV-0014.5 mobile money gateway scenarios passed.");
        AddScenarioCheck(
            summary,
            "Provider Integration",
            "scenario-provider-integration.log",
            "All AFW-DLV-0014.6 provider integration scenarios passed.");

        AddCheck(summary, "Configuration & Secrets", () =>
            FilesContain(
                [
                    "backend/src/PaymentPlatform/ProviderIntegration/ProviderIntegration.Infrastructure/Secrets/EnvironmentSecretSource.cs",
                    "docs/specs/provider-integration/secrets-guide.md"
                ],
                ["GetEnvironmentVariable", "must never be committed"]));

        AddCheck(summary, "Health Checks", () =>
            EveryRepositoryFileContains(ApiProgramPaths, "/health"));

        AddCheck(summary, "Logging & Correlation", () =>
            FilesContain(
                [
                    "backend/src/PaymentPlatform/ProviderIntegration/ProviderIntegration.Application/Contracts.cs",
                    "backend/src/PaymentPlatform/ProviderIntegration/ProviderIntegration.Application/ProviderIntegrationService.cs"
                ],
                ["CorrelationId"]));

        AddCheck(summary, "Audit Trail", () =>
            EvidenceContains(
                [
                    "scenario-payment-intent.log",
                    "scenario-payment-routing.log",
                    "scenario-merchant-acquiring.log",
                    "scenario-merchant-settlement.log",
                    "scenario-mobile-money.log",
                    "scenario-provider-integration.log"
                ],
                ["audit"]));

        AddCheck(summary, "Telemetry", () =>
            EvidenceContains(
                [
                    "scenario-payment-intent.log",
                    "scenario-payment-routing.log",
                    "scenario-merchant-acquiring.log",
                    "scenario-merchant-settlement.log",
                    "scenario-mobile-money.log",
                    "scenario-provider-integration.log"
                ],
                ["telemetry"]));

        AddCheck(summary, "Metrics", () =>
            FilesContain(
                [
                    "backend/src/PaymentPlatform/ProviderIntegration/ProviderIntegration.Domain/ProviderHealth.cs",
                    "backend/src/PaymentPlatform/ProviderIntegration/ProviderIntegration.Application/Contracts.cs",
                    "release/payment-platform/v1.4.0/dashboards/payment-platform-dashboard.md"
                ],
                ["AverageLatencyMs", "DurationMs", "success rate"]));

        AddCheck(summary, "Retry Policy", () =>
            EvidenceContains(
                ["scenario-provider-integration.log"],
                ["retry policy", "exception retry"]));

        AddCheck(summary, "Circuit Breaker", () =>
            EvidenceContains(
                ["scenario-provider-integration.log"],
                ["circuit breaker foundation"]));

        AddCheck(summary, "Provider Health", () =>
            EvidenceContains(
                ["scenario-provider-integration.log"],
                ["provider health"]));

        AddCheck(summary, "Webhook Verification", () =>
            EvidenceContains(
                ["scenario-provider-integration.log"],
                ["webhook signature", "invalid webhook rejected"]));

        AddCheck(summary, "Idempotency", () =>
            EvidenceContains(
                [
                    "scenario-payment-intent.log",
                    "scenario-payment-routing.log",
                    "scenario-merchant-acquiring.log",
                    "scenario-merchant-settlement.log",
                    "scenario-mobile-money.log"
                ],
                ["idempot"]));

        AddCheck(summary, "Failure Recovery", () =>
            EvidenceContainsByFile(
                [
                    ("scenario-payment-intent.log", "invalid transition"),
                    ("scenario-merchant-acquiring.log", "refund amount protection"),
                    ("scenario-provider-integration.log", "exception retry")
                ]));

        AddCheck(summary, "Release Build", () =>
            EvidenceContains(BuildEvidenceFiles, ["STEP_STATUS=PASS"]));

        AddCheck(summary, "Secret Scan", () =>
            EvidenceContains(["secret-scan.log"], ["Secret Scan PASS"]));

        AddCheck(summary, "Dependency Scan", () =>
            EvidenceContains(
                ["dependency-scan.log"],
                ["Dependency Vulnerability Scan PASS"]));

        AddCheck(summary, "Packaging", VerifyPackagingInputs);

        return summary;
    }

    private static string[] ApiProgramPaths =>
    [
        "backend/src/PaymentPlatform/PaymentIntent/PaymentIntent.Api/Program.cs",
        "backend/src/PaymentPlatform/PaymentRouting/PaymentRouting.Api/Program.cs",
        "backend/src/PaymentPlatform/MerchantAcquiring/MerchantAcquiring.Api/Program.cs",
        "backend/src/PaymentPlatform/MerchantSettlement/MerchantSettlement.Api/Program.cs",
        "backend/src/PaymentPlatform/MobileMoney/MobileMoney.Api/Program.cs",
        "backend/src/PaymentPlatform/ProviderIntegration/ProviderIntegration.Api/Program.cs"
    ];

    private static string[] BuildEvidenceFiles =>
    [
        "build-payment-intent.log",
        "build-payment-routing.log",
        "build-merchant-acquiring.log",
        "build-merchant-settlement.log",
        "build-mobile-money.log",
        "build-provider-integration.log",
        "build-payment-readiness.log"
    ];

    private void AddScenarioCheck(
        ReadinessSummary summary,
        string name,
        string evidenceFile,
        string successMarker)
    {
        AddCheck(summary, name, () =>
            EvidenceContains(
                [evidenceFile],
                ["STEP_STATUS=PASS", successMarker]));
    }

    private static void AddCheck(
        ReadinessSummary summary,
        string name,
        Func<(bool Passed, string Details)> check)
    {
        try
        {
            var result = check();
            summary.Add(name, result.Passed, result.Details);
        }
        catch (Exception exception)
        {
            summary.Add(name, false, exception.Message);
        }
    }

    private (bool Passed, string Details) EvidenceContains(
        IReadOnlyCollection<string> relativeFiles,
        IReadOnlyCollection<string> markers)
    {
        foreach (var relativeFile in relativeFiles)
        {
            var path = Path.Combine(_evidenceDirectory, relativeFile);

            if (!File.Exists(path))
                return (false, $"Missing evidence: {relativeFile}");

            var content = File.ReadAllText(path);

            foreach (var marker in markers)
            {
                if (!content.Contains(marker, StringComparison.OrdinalIgnoreCase))
                {
                    return (false, $"Evidence '{relativeFile}' lacks '{marker}'.");
                }
            }
        }

        return (true, $"Verified {relativeFiles.Count} evidence file(s).");
    }

    private (bool Passed, string Details) FilesContain(
        IReadOnlyCollection<string> relativeFiles,
        IReadOnlyCollection<string> markers)
    {
        var contents = new List<string>();

        foreach (var relativeFile in relativeFiles)
        {
            var path = Path.Combine(_repositoryRoot, relativeFile);

            if (!File.Exists(path))
                return (false, $"Missing repository file: {relativeFile}");

            contents.Add(File.ReadAllText(path));
        }

        foreach (var marker in markers)
        {
            if (!contents.Any(content =>
                    content.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, $"Repository evidence lacks '{marker}'.");
            }
        }

        return (true, $"Verified {relativeFiles.Count} repository file(s).");
    }

    private (bool Passed, string Details) EveryRepositoryFileContains(
        IReadOnlyCollection<string> relativeFiles,
        string marker)
    {
        foreach (var relativeFile in relativeFiles)
        {
            var path = Path.Combine(_repositoryRoot, relativeFile);

            if (!File.Exists(path))
                return (false, $"Missing repository file: {relativeFile}");

            if (!File.ReadAllText(path).Contains(
                    marker,
                    StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"Repository file '{relativeFile}' lacks '{marker}'.");
            }
        }

        return (true, $"Verified {relativeFiles.Count} repository file(s).");
    }

    private (bool Passed, string Details) EvidenceContainsByFile(
        IReadOnlyCollection<(string File, string Marker)> requirements)
    {
        foreach (var requirement in requirements)
        {
            var result = EvidenceContains(
                [requirement.File],
                [requirement.Marker]);

            if (!result.Passed)
                return result;
        }

        return (true, $"Verified {requirements.Count} recovery signal(s).");
    }

    private (bool Passed, string Details) VerifyPackagingInputs()
    {
        var requiredFiles = new[]
        {
            "release-notes.md",
            "configuration/release-metadata.json",
            "openapi/catalog.md",
            "runbooks/payment-platform-operations.md",
            "configuration/configuration-matrix.md",
            "dashboards/payment-platform-dashboard.md",
            "artifacts/release-evidence.md",
            "rollback/rollback-plan.md"
        };

        foreach (var relativeFile in requiredFiles)
        {
            var path = Path.Combine(_releaseDirectory, relativeFile);

            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                return (false, $"Missing package input: {relativeFile}");
        }

        return (true, $"Verified {requiredFiles.Length} package input(s).");
    }
}