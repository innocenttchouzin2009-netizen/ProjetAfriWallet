using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Application;
using MobileMoney.Production.Payout.Funding.Domain;

internal static class FundingExecutionRecoveryScenarios
{
    public static async Task RunAllAsync()
    {
        await RunAsync(
            "recovery resolves processing attempt to succeeded",
            ResolvesProcessingAttemptToSucceeded);

        await RunAsync(
            "recovery resolves processing attempt to failed",
            ResolvesProcessingAttemptToFailed);

        await RunAsync(
            "still-processing recovery remains non-mutating",
            StillProcessingRemainsNonMutating);

        await RunAsync(
            "missing recovery probe fails before provider calls",
            MissingProbeFailsBeforeProviderCalls);

        await RunAsync(
            "duplicate recovery probes fail before provider calls",
            DuplicateProbesFailBeforeProviderCalls);

        await RunAsync(
            "terminal recovery replay does not query provider",
            TerminalReplayDoesNotQueryProvider);

        await RunAsync(
            "invalid recovery provider result is rejected",
            InvalidProviderResultIsRejected);

        Console.WriteLine(
            "Mobile Money payout funding execution recovery scenarios passed.");
    }

    private static async Task ResolvesProcessingAttemptToSucceeded()
    {
        var correlationId = Guid.NewGuid();
        var createdAt = Utc(2026, 10, 10, 14, 0, 0);
        var attempt = ProcessingAttempt(
            correlationId,
            "wallet-main",
            FundingSourceType.Wallet,
            5_000,
            createdAt);

        var store = new RecoveryAttemptStore(attempt);
        var probe = FakeRecoveryProbe.Success(
            FundingSourceType.Wallet,
            "wallet-debit-recovered-001");
        var clock = new FixedTimeProvider(createdAt.AddMinutes(2));

        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var result = await CreateService(store, clock, probe).RecoverAsync(
            new RecoverMobileMoneyPayoutFundingCommand(correlationId),
            token);

        Assert(
            result.Attempts.Single().Status == FundingAttemptStatus.Succeeded,
            "Recovered attempt must succeed.");
        Assert(
            attempt.ProviderReference == "wallet-debit-recovered-001",
            "Recovered provider reference mismatch.");
        Assert(
            store.Snapshots.Count == 1 &&
            store.Snapshots[0].Status == FundingAttemptStatus.Succeeded,
            "Recovered success must persist exactly once.");
        Assert(
            probe.Requests.Count == 1 &&
            probe.Requests[0].IdempotencyKey == attempt.ExecutionIdempotencyKey,
            "Recovery must query with the stable execution idempotency key.");
        Assert(
            probe.LastToken == token && store.LastToken == token,
            "Cancellation token must reach recovery ports.");
    }

    private static async Task ResolvesProcessingAttemptToFailed()
    {
        var correlationId = Guid.NewGuid();
        var createdAt = Utc(2026, 10, 10, 15, 0, 0);
        var attempt = ProcessingAttempt(
            correlationId,
            "card-main",
            FundingSourceType.PaymentCard,
            6_000,
            createdAt);

        var store = new RecoveryAttemptStore(attempt);
        var probe = FakeRecoveryProbe.Failure(
            FundingSourceType.PaymentCard,
            "provider-declined",
            "card-charge-recovered-002");

        await CreateService(
                store,
                new FixedTimeProvider(createdAt.AddMinutes(3)),
                probe)
            .RecoverAsync(
                new RecoverMobileMoneyPayoutFundingCommand(correlationId));

        Assert(
            attempt.Status == FundingAttemptStatus.Failed,
            "Recovered failed attempt must enter Failed.");
        Assert(
            attempt.StatusReason == "provider-declined",
            "Recovered failure code must become the attempt reason.");
        Assert(
            attempt.ProviderReference == "card-charge-recovered-002",
            "Recovered failure must retain provider reference.");
    }

    private static async Task StillProcessingRemainsNonMutating()
    {
        var correlationId = Guid.NewGuid();
        var createdAt = Utc(2026, 10, 10, 16, 0, 0);
        var attempt = ProcessingAttempt(
            correlationId,
            "sepa-main",
            FundingSourceType.Sepa,
            7_000,
            createdAt);

        var store = new RecoveryAttemptStore(attempt);
        var probe = FakeRecoveryProbe.StillProcessing(
            FundingSourceType.Sepa);

        await CreateService(
                store,
                new FixedTimeProvider(createdAt.AddMinutes(4)),
                probe)
            .RecoverAsync(
                new RecoverMobileMoneyPayoutFundingCommand(correlationId));

        Assert(
            attempt.Status == FundingAttemptStatus.Processing,
            "Unknown provider outcome must remain Processing.");
        Assert(
            store.Snapshots.Count == 0,
            "Still-processing recovery must not rewrite the attempt.");
        Assert(
            probe.Requests.Count == 1,
            "Recovery probe must be queried once.");
    }

    private static async Task MissingProbeFailsBeforeProviderCalls()
    {
        var correlationId = Guid.NewGuid();
        var createdAt = Utc(2026, 10, 10, 17, 0, 0);
        var wallet = ProcessingAttempt(
            correlationId,
            "wallet-main",
            FundingSourceType.Wallet,
            3_000,
            createdAt);
        var applePay = ProcessingAttempt(
            correlationId,
            "apple-pay",
            FundingSourceType.ApplePay,
            4_000,
            createdAt);

        var store = new RecoveryAttemptStore(wallet, applePay);
        var walletProbe = FakeRecoveryProbe.Success(
            FundingSourceType.Wallet,
            "wallet-should-not-run");

        await ExpectAsync<InvalidOperationException>(() =>
            CreateService(
                    store,
                    new FixedTimeProvider(createdAt.AddMinutes(5)),
                    walletProbe)
                .RecoverAsync(
                    new RecoverMobileMoneyPayoutFundingCommand(correlationId)));

        Assert(
            walletProbe.Requests.Count == 0,
            "Probe coverage must be validated before any recovery query.");
        Assert(
            store.Snapshots.Count == 0,
            "Missing probe must fail before persistence.");
        Assert(
            wallet.Status == FundingAttemptStatus.Processing &&
            applePay.Status == FundingAttemptStatus.Processing,
            "Missing probe must not mutate attempts.");
    }

    private static async Task DuplicateProbesFailBeforeProviderCalls()
    {
        var correlationId = Guid.NewGuid();
        var createdAt = Utc(2026, 10, 10, 18, 0, 0);
        var attempt = ProcessingAttempt(
            correlationId,
            "wallet-main",
            FundingSourceType.Wallet,
            5_000,
            createdAt);

        var first = FakeRecoveryProbe.Success(
            FundingSourceType.Wallet,
            "wallet-first");
        var second = FakeRecoveryProbe.Success(
            FundingSourceType.Wallet,
            "wallet-second");

        await ExpectAsync<InvalidOperationException>(() =>
            CreateService(
                    new RecoveryAttemptStore(attempt),
                    new FixedTimeProvider(createdAt.AddMinutes(6)),
                    first,
                    second)
                .RecoverAsync(
                    new RecoverMobileMoneyPayoutFundingCommand(correlationId)));

        Assert(
            first.Requests.Count == 0 && second.Requests.Count == 0,
            "Ambiguous probe routing must fail before provider queries.");
    }

    private static async Task TerminalReplayDoesNotQueryProvider()
    {
        var correlationId = Guid.NewGuid();
        var createdAt = Utc(2026, 10, 10, 19, 0, 0);
        var attempt = ProcessingAttempt(
            correlationId,
            "wallet-main",
            FundingSourceType.Wallet,
            5_000,
            createdAt);
        attempt.MarkSucceeded(
            createdAt.AddMinutes(1),
            "wallet-already-complete");

        var probe = FakeRecoveryProbe.Success(
            FundingSourceType.Wallet,
            "should-not-run");
        var store = new RecoveryAttemptStore(attempt);

        var result = await CreateService(
                store,
                new FixedTimeProvider(createdAt.AddMinutes(7)),
                probe)
            .RecoverAsync(
                new RecoverMobileMoneyPayoutFundingCommand(correlationId));

        Assert(
            result.Attempts.Single().Status == FundingAttemptStatus.Succeeded,
            "Terminal recovery replay must preserve success.");
        Assert(
            probe.Requests.Count == 0,
            "Terminal attempts must not query recovery providers.");
        Assert(
            store.Snapshots.Count == 0,
            "Terminal replay must not rewrite state.");
    }

    private static async Task InvalidProviderResultIsRejected()
    {
        var correlationId = Guid.NewGuid();
        var createdAt = Utc(2026, 10, 10, 20, 0, 0);
        var attempt = ProcessingAttempt(
            correlationId,
            "card-main",
            FundingSourceType.PaymentCard,
            5_000,
            createdAt);

        var store = new RecoveryAttemptStore(attempt);
        var probe = FakeRecoveryProbe.InvalidSuccess(
            FundingSourceType.PaymentCard);

        await ExpectAsync<InvalidOperationException>(() =>
            CreateService(
                    store,
                    new FixedTimeProvider(createdAt.AddMinutes(8)),
                    probe)
                .RecoverAsync(
                    new RecoverMobileMoneyPayoutFundingCommand(correlationId)));

        Assert(
            attempt.Status == FundingAttemptStatus.Processing,
            "Invalid provider result must leave attempt Processing.");
        Assert(
            store.Snapshots.Count == 0,
            "Invalid provider result must not persist a terminal state.");
    }

    private static MobileMoneyPayoutFundingRecoveryService CreateService(
        IMobileMoneyPayoutFundingAttemptStore store,
        TimeProvider timeProvider,
        params IMobileMoneyPayoutFundingRecoveryProbe[] probes) =>
        new(store, probes, timeProvider);

    private static FundingAttempt ProcessingAttempt(
        Guid correlationId,
        string sourceId,
        FundingSourceType sourceType,
        long amountMinor,
        DateTimeOffset createdAtUtc)
    {
        var attempt = FundingAttempt.Create(
            Guid.NewGuid(),
            correlationId,
            FundingAllocation.Create(
                sourceId,
                sourceType,
                amountMinor,
                "EUR"),
            createdAtUtc);

        attempt.MarkProcessing(createdAtUtc.AddSeconds(1));
        return attempt;
    }

    private static DateTimeOffset Utc(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second) =>
        new(
            year,
            month,
            day,
            hour,
            minute,
            second,
            TimeSpan.Zero);

    private static async Task RunAsync(
        string name,
        Func<Task> action)
    {
        await action();
        Console.WriteLine($"PASS: {name}");
    }

    private static async Task ExpectAsync<T>(
        Func<Task> action)
        where T : Exception
    {
        try
        {
            await action();
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Expected {typeof(T).Name}.");
    }

    private static void Assert(
        bool condition,
        string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class RecoveryAttemptStore
        : IMobileMoneyPayoutFundingAttemptStore
    {
        private readonly List<FundingAttempt> _attempts;

        public RecoveryAttemptStore(
            params FundingAttempt[] attempts)
        {
            _attempts = attempts.ToList();
        }

        public List<AttemptSnapshot> Snapshots { get; } = new();

        public CancellationToken LastToken { get; private set; }

        public Task SaveAsync(
            FundingAttempt attempt,
            CancellationToken cancellationToken = default)
        {
            LastToken = cancellationToken;
            Snapshots.Add(
                new AttemptSnapshot(
                    attempt.Id,
                    attempt.Status,
                    attempt.StatusReason,
                    attempt.ProviderReference));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FundingAttempt>> FindByCorrelationIdAsync(
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            LastToken = cancellationToken;

            IReadOnlyList<FundingAttempt> result = _attempts
                .Where(attempt =>
                    attempt.CorrelationId == correlationId)
                .ToArray();

            return Task.FromResult(result);
        }
    }

    private sealed record AttemptSnapshot(
        Guid AttemptId,
        FundingAttemptStatus Status,
        string? StatusReason,
        string? ProviderReference);

    private sealed class FakeRecoveryProbe
        : IMobileMoneyPayoutFundingRecoveryProbe
    {
        private readonly FundingSourceType _sourceType;
        private readonly Func<FundingRecoveryProviderResult> _factory;

        private FakeRecoveryProbe(
            FundingSourceType sourceType,
            Func<FundingRecoveryProviderResult> factory)
        {
            _sourceType = sourceType;
            _factory = factory;
        }

        public List<FundingRecoveryProviderRequest> Requests { get; } = new();

        public CancellationToken LastToken { get; private set; }

        public bool Supports(FundingSourceType sourceType) =>
            sourceType == _sourceType;

        public Task<FundingRecoveryProviderResult> RecoverAsync(
            FundingRecoveryProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            LastToken = cancellationToken;
            Requests.Add(request);
            return Task.FromResult(_factory());
        }

        public static FakeRecoveryProbe Success(
            FundingSourceType sourceType,
            string providerReference) =>
            new(
                sourceType,
                () => new FundingRecoveryProviderResult(
                    FundingRecoveryDisposition.Succeeded,
                    providerReference,
                    null));

        public static FakeRecoveryProbe Failure(
            FundingSourceType sourceType,
            string failureCode,
            string? providerReference = null) =>
            new(
                sourceType,
                () => new FundingRecoveryProviderResult(
                    FundingRecoveryDisposition.Failed,
                    providerReference,
                    failureCode));

        public static FakeRecoveryProbe StillProcessing(
            FundingSourceType sourceType) =>
            new(
                sourceType,
                () => new FundingRecoveryProviderResult(
                    FundingRecoveryDisposition.StillProcessing,
                    null,
                    null));

        public static FakeRecoveryProbe InvalidSuccess(
            FundingSourceType sourceType) =>
            new(
                sourceType,
                () => new FundingRecoveryProviderResult(
                    FundingRecoveryDisposition.Succeeded,
                    null,
                    "unexpected-failure-code"));
    }

    private sealed class FixedTimeProvider(
        DateTimeOffset now)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
