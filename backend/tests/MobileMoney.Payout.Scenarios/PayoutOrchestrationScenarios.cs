using MobileMoney.Production.Payout.Abstractions;
using MobileMoney.Production.Payout.Application;
using MobileMoney.Production.Payout.Contracts;
using MobileMoney.Production.Payout.Domain;

internal static class PayoutOrchestrationScenarios
{
    public static void RunAll()
    {
        var scenarios = new (string Name, Action Run)[]
        {
            ("orchestrator persists and submits accepted payout", AcceptedPayout),
            ("provider rejection marks payout failed", RejectedPayout),
            ("idempotent replay avoids duplicate provider submission", IdempotentReplay),
            ("unsupported corridor is rejected before provider submission", UnsupportedCorridor),
            ("unsupported operator is rejected without silent substitution", UnsupportedOperator),
            ("disabled corridor is rejected before provider submission", DisabledCorridor),
            ("collection-only capability cannot authorize payout", CollectionOnlyCapability)
        };

        foreach (var scenario in scenarios)
        {
            scenario.Run();
            Console.WriteLine($"PASS: {scenario.Name}");
        }

        Console.WriteLine(
            $"MobileMoney payout orchestration scenarios: {scenarios.Length}/{scenarios.Length} passed.");
    }

    private static void AcceptedPayout()
    {
        var store = new ScenarioPayoutStore();
        var provider = new ScenarioPayoutProvider(
            MobileMoneyPayoutSubmissionResult.Success("provider-ref-001"));
        var orchestrator = CreateOrchestrator(
            store,
            provider,
            EligiblePolicy(),
            Utc(12, 0),
            Utc(12, 1),
            Utc(12, 2));

        var response = orchestrator.CreateAndSubmitAsync(
            ValidRequest("key-accepted")).GetAwaiter().GetResult();

        AssertEqual(MobileMoneyPayoutStatus.Submitted, response.Status);
        AssertEqual("provider-ref-001", response.ProviderReference);
        Assert(response.FailureCode is null, "Accepted payout must not have a failure code.");
        AssertEqual(1, provider.SubmissionCount);
        AssertEqual(3, store.SavedStatuses.Count);
        AssertEqual(MobileMoneyPayoutStatus.Created, store.SavedStatuses[0]);
        AssertEqual(MobileMoneyPayoutStatus.Processing, store.SavedStatuses[1]);
        AssertEqual(MobileMoneyPayoutStatus.Submitted, store.SavedStatuses[2]);
        AssertEqual(response.PayoutId, provider.LastSubmission!.PayoutId);
        AssertEqual("DE", provider.LastSubmission.SourceCountryCode);
        AssertEqual("EUR", provider.LastSubmission.SourceCurrency);
        AssertEqual(25_000L, provider.LastSubmission.AmountMinor);
        AssertEqual("XAF", provider.LastSubmission.Currency);
    }

    private static void RejectedPayout()
    {
        var store = new ScenarioPayoutStore();
        var provider = new ScenarioPayoutProvider(
            MobileMoneyPayoutSubmissionResult.Rejected("PROVIDER_REJECTED"));
        var orchestrator = CreateOrchestrator(
            store,
            provider,
            EligiblePolicy(),
            Utc(13, 0),
            Utc(13, 1),
            Utc(13, 2));

        var response = orchestrator.CreateAndSubmitAsync(
            ValidRequest("key-rejected")).GetAwaiter().GetResult();

        AssertEqual(MobileMoneyPayoutStatus.Failed, response.Status);
        AssertEqual("PROVIDER_REJECTED", response.FailureCode);
        Assert(response.ProviderReference is null, "Rejected payout must not have a provider reference.");
        AssertEqual(1, provider.SubmissionCount);
        AssertEqual(MobileMoneyPayoutStatus.Failed, store.SavedStatuses[^1]);
    }

    private static void IdempotentReplay()
    {
        var createdAt = Utc(14, 0);
        var existing = MobileMoneyPayout.Create(
            "wallet-001",
            25_000,
            "XAF",
            new MobileMoneyBeneficiary(
                "+237690000001",
                "CM",
                "MTN-CM",
                "Ada"),
            "key-existing",
            createdAt);

        existing.Start(Utc(14, 1));
        existing.MarkSubmitted("provider-existing", Utc(14, 2));

        var store = new ScenarioPayoutStore(existing);
        var provider = new ScenarioPayoutProvider(
            MobileMoneyPayoutSubmissionResult.Success("should-not-be-used"));
        var orchestrator = CreateOrchestrator(
            store,
            provider,
            new ConfiguredMobileMoneyPayoutEligibilityPolicy([]));

        var response = orchestrator.CreateAndSubmitAsync(
            ValidRequest("  key-existing  ")).GetAwaiter().GetResult();

        AssertEqual(existing.PayoutId, response.PayoutId);
        AssertEqual(MobileMoneyPayoutStatus.Submitted, response.Status);
        AssertEqual("provider-existing", response.ProviderReference);
        AssertEqual(0, provider.SubmissionCount);
        AssertEqual(0, store.SavedStatuses.Count);
    }

    private static void UnsupportedCorridor()
    {
        AssertEligibilityDenied(
            ValidRequest("key-corridor", sourceCountryCode: "FR"),
            EligiblePolicy(),
            MobileMoneyPayoutEligibilityCodes.CorridorNotSupported);
    }

    private static void UnsupportedOperator()
    {
        AssertEligibilityDenied(
            ValidRequest("key-operator", operatorCode: "ORANGE-CM"),
            EligiblePolicy(),
            MobileMoneyPayoutEligibilityCodes.OperatorNotActivated);
    }

    private static void DisabledCorridor()
    {
        AssertEligibilityDenied(
            ValidRequest("key-disabled"),
            Policy(enabled: false, collectionEnabled: true, outboundPayoutEnabled: true),
            MobileMoneyPayoutEligibilityCodes.CorridorDisabled);
    }

    private static void CollectionOnlyCapability()
    {
        AssertEligibilityDenied(
            ValidRequest("key-collection-only"),
            Policy(enabled: true, collectionEnabled: true, outboundPayoutEnabled: false),
            MobileMoneyPayoutEligibilityCodes.OutboundPayoutCapabilityRequired);
    }

    private static void AssertEligibilityDenied(
        CreateMobileMoneyPayoutRequest request,
        IMobileMoneyPayoutEligibilityPolicy policy,
        string expectedCode)
    {
        var store = new ScenarioPayoutStore();
        var provider = new ScenarioPayoutProvider(
            MobileMoneyPayoutSubmissionResult.Success("must-not-submit"));
        var orchestrator = CreateOrchestrator(store, provider, policy);

        try
        {
            orchestrator.CreateAndSubmitAsync(request).GetAwaiter().GetResult();
        }
        catch (MobileMoneyPayoutEligibilityException exception)
        {
            AssertEqual(expectedCode, exception.Code);
            AssertEqual(0, provider.SubmissionCount);
            AssertEqual(0, store.SavedStatuses.Count);
            return;
        }

        throw new InvalidOperationException(
            $"Expected eligibility failure '{expectedCode}'.");
    }

    private static MobileMoneyPayoutOrchestrator CreateOrchestrator(
        ScenarioPayoutStore store,
        ScenarioPayoutProvider provider,
        IMobileMoneyPayoutEligibilityPolicy policy,
        params DateTimeOffset[] times) =>
        new(
            store,
            provider,
            new ScenarioPayoutClock(times),
            policy);

    private static IMobileMoneyPayoutEligibilityPolicy EligiblePolicy() =>
        Policy(enabled: true, collectionEnabled: true, outboundPayoutEnabled: true);

    private static IMobileMoneyPayoutEligibilityPolicy Policy(
        bool enabled,
        bool collectionEnabled,
        bool outboundPayoutEnabled) =>
        new ConfiguredMobileMoneyPayoutEligibilityPolicy(
        [
            new MobileMoneyPayoutCorridorCapability(
                new MobileMoneyPayoutCorridor(
                    "DE",
                    "EUR",
                    "CM",
                    "XAF",
                    "MTN-CM"),
                enabled,
                collectionEnabled,
                outboundPayoutEnabled)
        ]);

    private static CreateMobileMoneyPayoutRequest ValidRequest(
        string idempotencyKey,
        string sourceCountryCode = "DE",
        string sourceCurrency = "EUR",
        string destinationCountryCode = "CM",
        string operatorCode = "MTN-CM") =>
        new(
            "wallet-001",
            sourceCountryCode,
            sourceCurrency,
            25_000,
            "xaf",
            new MobileMoneyBeneficiaryRequest(
                "+237690000001",
                destinationCountryCode,
                operatorCode,
                "Ada"),
            idempotencyKey);

    private static DateTimeOffset Utc(int hour, int minute) =>
        new(2026, 10, 3, hour, minute, 0, TimeSpan.Zero);

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"Expected '{expected}', got '{actual}'.");
        }
    }

    private sealed class ScenarioPayoutStore : IMobileMoneyPayoutStore
    {
        private readonly Dictionary<string, MobileMoneyPayout> _byIdempotency =
            new(StringComparer.Ordinal);

        public ScenarioPayoutStore(params MobileMoneyPayout[] payouts)
        {
            foreach (var payout in payouts)
                _byIdempotency[payout.IdempotencyKey] = payout;
        }

        public List<MobileMoneyPayoutStatus> SavedStatuses { get; } = [];

        public Task<MobileMoneyPayout?> FindByIdempotencyKeyAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _byIdempotency.TryGetValue(idempotencyKey, out var payout);
            return Task.FromResult(payout);
        }

        public Task SaveAsync(
            MobileMoneyPayout payout,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _byIdempotency[payout.IdempotencyKey] = payout;
            SavedStatuses.Add(payout.Status);
            return Task.CompletedTask;
        }
    }

    private sealed class ScenarioPayoutProvider : IMobileMoneyPayoutProvider
    {
        private readonly MobileMoneyPayoutSubmissionResult _result;

        public ScenarioPayoutProvider(
            MobileMoneyPayoutSubmissionResult result)
        {
            _result = result;
        }

        public int SubmissionCount { get; private set; }
        public MobileMoneyPayoutSubmission? LastSubmission { get; private set; }

        public Task<MobileMoneyPayoutSubmissionResult> SubmitAsync(
            MobileMoneyPayoutSubmission submission,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SubmissionCount++;
            LastSubmission = submission;
            return Task.FromResult(_result);
        }
    }

    private sealed class ScenarioPayoutClock : IMobileMoneyPayoutClock
    {
        private readonly Queue<DateTimeOffset> _times;

        public ScenarioPayoutClock(params DateTimeOffset[] times)
        {
            _times = new Queue<DateTimeOffset>(times);
        }

        public DateTimeOffset UtcNow =>
            _times.Count > 0
                ? _times.Dequeue()
                : throw new InvalidOperationException(
                    "No clock value was expected for this scenario.");
    }
}
