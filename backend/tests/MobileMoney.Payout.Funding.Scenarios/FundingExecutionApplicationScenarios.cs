using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Application;
using MobileMoney.Production.Payout.Funding.Domain;

internal static class FundingExecutionApplicationScenarios
{
    public static async Task RunAllAsync()
    {
        await RunAsync(
            "execution dispatches planned attempts through matching executors",
            ExecutesPlannedAttempts);

        await RunAsync(
            "provider failure stops remaining planned attempts",
            ProviderFailureStopsRemainingAttempts);

        await RunAsync(
            "missing executor fails before any attempt transition",
            MissingExecutorFailsBeforeMutation);

        await RunAsync(
            "processing attempt blocks duplicate execution",
            ProcessingAttemptBlocksReentry);

        await RunAsync(
            "completed execution replays without provider calls",
            CompletedExecutionReplaysWithoutProviderCalls);

        await RunAsync(
            "executor exception leaves attempt processing for recovery",
            ExecutorExceptionLeavesAttemptProcessing);

        Console.WriteLine(
            "Mobile Money payout funding execution application scenarios passed.");
    }

    private static async Task ExecutesPlannedAttempts()
    {
        var correlationId = Guid.NewGuid();
        var createdAt = Utc(2026, 10, 10, 8, 0, 0);

        var walletAttempt = Attempt(
            correlationId,
            "wallet-main",
            FundingSourceType.Wallet,
            4_000,
            createdAt);

        var cardAttempt = Attempt(
            correlationId,
            "card-main",
            FundingSourceType.PaymentCard,
            6_000,
            createdAt);

        var store = new ExecutionAttemptStore(
            walletAttempt,
            cardAttempt);

        var walletExecutor = FakeExecutor.Success(
            FundingSourceType.Wallet,
            "wallet-debit-001");

        var cardExecutor = FakeExecutor.Success(
            FundingSourceType.PaymentCard,
            "card-charge-001");

        var clock = new IncrementingTimeProvider(
            createdAt.AddSeconds(1));

        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var service = CreateService(
            store,
            clock,
            walletExecutor,
            cardExecutor);

        var result = await service.ExecuteAsync(
            new ExecuteMobileMoneyPayoutFundingCommand(correlationId),
            token);

        Assert(
            result.CorrelationId == correlationId,
            "Execution result correlation mismatch.");

        Assert(
            result.Attempts.All(x => x.Status == FundingAttemptStatus.Succeeded),
            "All planned attempts must succeed.");

        Assert(
            walletAttempt.ProviderReference == "wallet-debit-001",
            "Wallet provider reference mismatch.");

        Assert(
            cardAttempt.ProviderReference == "card-charge-001",
            "Card provider reference mismatch.");

        Assert(
            walletExecutor.Requests.Count == 1 &&
            cardExecutor.Requests.Count == 1,
            "Each matching executor must be invoked exactly once.");

        Assert(
            walletExecutor.Requests[0].IdempotencyKey ==
                walletAttempt.ExecutionIdempotencyKey,
            "Execution must propagate the stable attempt idempotency key.");

        Assert(
            walletExecutor.LastToken == token &&
            cardExecutor.LastToken == token &&
            store.LastToken == token,
            "Cancellation token must reach execution ports.");

        Assert(
            store.Snapshots.Count == 4,
            "Each attempt must persist Processing and terminal status.");

        Assert(
            store.Snapshots.Count(x =>
                x.Status == FundingAttemptStatus.Processing) == 2,
            "Each attempt must persist Processing exactly once.");

        Assert(
            store.Snapshots.Count(x =>
                x.Status == FundingAttemptStatus.Succeeded) == 2,
            "Each attempt must persist Succeeded exactly once.");
    }

    private static async Task ProviderFailureStopsRemainingAttempts()
    {
        var correlationId = Guid.NewGuid();
        var createdAt = Utc(2026, 10, 10, 9, 0, 0);

        var walletAttempt = Attempt(
            correlationId,
            "wallet-main",
            FundingSourceType.Wallet,
            4_000,
            createdAt);

        var cardAttempt = Attempt(
            correlationId,
            "card-main",
            FundingSourceType.PaymentCard,
            6_000,
            createdAt);

        var store = new ExecutionAttemptStore(
            walletAttempt,
            cardAttempt);

        var walletExecutor = FakeExecutor.Failure(
            FundingSourceType.Wallet,
            "wallet-debit-declined",
            "wallet-debit-002");

        var cardExecutor = FakeExecutor.Success(
            FundingSourceType.PaymentCard,
            "card-charge-002");

        var service = CreateService(
            store,
            new IncrementingTimeProvider(createdAt.AddSeconds(1)),
            walletExecutor,
            cardExecutor);

        var result = await service.ExecuteAsync(
            new ExecuteMobileMoneyPayoutFundingCommand(correlationId));

        Assert(
            walletAttempt.Status == FundingAttemptStatus.Failed,
            "Failed provider result must fail its attempt.");

        Assert(
            walletAttempt.StatusReason == "wallet-debit-declined",
            "Provider failure code must become the attempt reason.");

        Assert(
            walletAttempt.ProviderReference == "wallet-debit-002",
            "Failed attempt must retain the provider reference.");

        Assert(
            cardAttempt.Status == FundingAttemptStatus.Planned,
            "Execution must stop before the next allocation after failure.");

        Assert(
            cardExecutor.Requests.Count == 0,
            "Later executors must not run after a funding failure.");

        Assert(
            result.Attempts.Count == 2,
            "Execution result must retain the complete attempt set.");
    }

    private static async Task MissingExecutorFailsBeforeMutation()
    {
        var correlationId = Guid.NewGuid();
        var createdAt = Utc(2026, 10, 10, 10, 0, 0);

        var walletAttempt = Attempt(
            correlationId,
            "wallet-main",
            FundingSourceType.Wallet,
            4_000,
            createdAt);

        var applePayAttempt = Attempt(
            correlationId,
            "apple-pay",
            FundingSourceType.ApplePay,
            6_000,
            createdAt);

        var store = new ExecutionAttemptStore(
            walletAttempt,
            applePayAttempt);

        var walletExecutor = FakeExecutor.Success(
            FundingSourceType.Wallet,
            "wallet-debit-003");

        var service = CreateService(
            store,
            new IncrementingTimeProvider(createdAt.AddSeconds(1)),
            walletExecutor);

        await ExpectAsync<InvalidOperationException>(() =>
            service.ExecuteAsync(
                new ExecuteMobileMoneyPayoutFundingCommand(correlationId)));

        Assert(
            walletAttempt.Status == FundingAttemptStatus.Planned &&
            applePayAttempt.Status == FundingAttemptStatus.Planned,
            "Executor coverage must be validated before any attempt mutates.");

        Assert(
            store.Snapshots.Count == 0,
            "Missing executor must fail before persistence.");

        Assert(
            walletExecutor.Requests.Count == 0,
            "No executor may run when the execution plan is incomplete.");
    }

    private static async Task ProcessingAttemptBlocksReentry()
    {
        var correlationId = Guid.NewGuid();
        var createdAt = Utc(2026, 10, 10, 11, 0, 0);

        var attempt = Attempt(
            correlationId,
            "wallet-main",
            FundingSourceType.Wallet,
            5_000,
            createdAt);

        attempt.MarkProcessing(createdAt.AddSeconds(1));

        var store = new ExecutionAttemptStore(attempt);
        var executor = FakeExecutor.Success(
            FundingSourceType.Wallet,
            "wallet-debit-004");

        var service = CreateService(
            store,
            new IncrementingTimeProvider(createdAt.AddSeconds(2)),
            executor);

        await ExpectAsync<InvalidOperationException>(() =>
            service.ExecuteAsync(
                new ExecuteMobileMoneyPayoutFundingCommand(correlationId)));

        Assert(
            executor.Requests.Count == 0,
            "In-flight attempts must not be executed twice.");

        Assert(
            store.Snapshots.Count == 0,
            "Re-entry rejection must not mutate persisted state.");
    }

    private static async Task CompletedExecutionReplaysWithoutProviderCalls()
    {
        var correlationId = Guid.NewGuid();
        var createdAt = Utc(2026, 10, 10, 12, 0, 0);

        var attempt = Attempt(
            correlationId,
            "wallet-main",
            FundingSourceType.Wallet,
            5_000,
            createdAt);

        attempt.MarkProcessing(createdAt.AddSeconds(1));
        attempt.MarkSucceeded(
            createdAt.AddSeconds(2),
            "wallet-debit-005");

        var store = new ExecutionAttemptStore(attempt);
        var executor = FakeExecutor.Success(
            FundingSourceType.Wallet,
            "should-not-run");

        var service = CreateService(
            store,
            new IncrementingTimeProvider(createdAt.AddSeconds(3)),
            executor);

        var result = await service.ExecuteAsync(
            new ExecuteMobileMoneyPayoutFundingCommand(correlationId));

        Assert(
            result.Attempts.Single().Status == FundingAttemptStatus.Succeeded,
            "Completed execution must preserve terminal success.");

        Assert(
            executor.Requests.Count == 0,
            "Completed attempts must not execute again.");

        Assert(
            store.Snapshots.Count == 0,
            "Completed replay must not rewrite terminal state.");
    }

    private static async Task ExecutorExceptionLeavesAttemptProcessing()
    {
        var correlationId = Guid.NewGuid();
        var createdAt = Utc(2026, 10, 10, 13, 0, 0);

        var attempt = Attempt(
            correlationId,
            "sepa-main",
            FundingSourceType.Sepa,
            5_000,
            createdAt);

        var store = new ExecutionAttemptStore(attempt);
        var executor = FakeExecutor.Throwing(
            FundingSourceType.Sepa,
            new InvalidOperationException("rail-timeout"));

        var service = CreateService(
            store,
            new IncrementingTimeProvider(createdAt.AddSeconds(1)),
            executor);

        await ExpectAsync<InvalidOperationException>(() =>
            service.ExecuteAsync(
                new ExecuteMobileMoneyPayoutFundingCommand(correlationId)));

        Assert(
            attempt.Status == FundingAttemptStatus.Processing,
            "Unknown executor outcome must remain Processing for recovery.");

        Assert(
            store.Snapshots.Count == 1 &&
            store.Snapshots[0].Status == FundingAttemptStatus.Processing,
            "Processing must be persisted before invoking the executor.");
    }

    private static MobileMoneyPayoutFundingExecutionService CreateService(
        IMobileMoneyPayoutFundingAttemptStore store,
        TimeProvider timeProvider,
        params IMobileMoneyPayoutFundingExecutor[] executors) =>
        new(store, executors, timeProvider);

    private static FundingAttempt Attempt(
        Guid correlationId,
        string sourceId,
        FundingSourceType sourceType,
        long amountMinor,
        DateTimeOffset createdAtUtc) =>
        FundingAttempt.Create(
            Guid.NewGuid(),
            correlationId,
            FundingAllocation.Create(
                sourceId,
                sourceType,
                amountMinor,
                "EUR"),
            createdAtUtc);

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

    private sealed class ExecutionAttemptStore
        : IMobileMoneyPayoutFundingAttemptStore
    {
        private readonly List<FundingAttempt> _attempts;

        public ExecutionAttemptStore(
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

    private sealed class FakeExecutor
        : IMobileMoneyPayoutFundingExecutor
    {
        private readonly FundingSourceType _sourceType;
        private readonly Func<
            FundingExecutionProviderRequest,
            CancellationToken,
            Task<FundingExecutionProviderResult>> _handler;

        private FakeExecutor(
            FundingSourceType sourceType,
            Func<
                FundingExecutionProviderRequest,
                CancellationToken,
                Task<FundingExecutionProviderResult>> handler)
        {
            _sourceType = sourceType;
            _handler = handler;
        }

        public List<FundingExecutionProviderRequest> Requests { get; } = new();

        public CancellationToken LastToken { get; private set; }

        public bool Supports(FundingSourceType sourceType) =>
            sourceType == _sourceType;

        public async Task<FundingExecutionProviderResult> ExecuteAsync(
            FundingExecutionProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            LastToken = cancellationToken;
            Requests.Add(request);
            return await _handler(request, cancellationToken);
        }

        public static FakeExecutor Success(
            FundingSourceType sourceType,
            string providerReference) =>
            new(
                sourceType,
                (_, _) => Task.FromResult(
                    new FundingExecutionProviderResult(
                        true,
                        providerReference,
                        null)));

        public static FakeExecutor Failure(
            FundingSourceType sourceType,
            string failureCode,
            string? providerReference = null) =>
            new(
                sourceType,
                (_, _) => Task.FromResult(
                    new FundingExecutionProviderResult(
                        false,
                        providerReference,
                        failureCode)));

        public static FakeExecutor Throwing(
            FundingSourceType sourceType,
            Exception exception) =>
            new(
                sourceType,
                (_, _) => Task.FromException<FundingExecutionProviderResult>(
                    exception));
    }

    private sealed class IncrementingTimeProvider
        : TimeProvider
    {
        private DateTimeOffset _next;

        public IncrementingTimeProvider(
            DateTimeOffset firstTimestampUtc)
        {
            if (firstTimestampUtc.Offset != TimeSpan.Zero)
            {
                throw new ArgumentException(
                    "Test time provider requires UTC.",
                    nameof(firstTimestampUtc));
            }

            _next = firstTimestampUtc;
        }

        public override DateTimeOffset GetUtcNow()
        {
            var current = _next;
            _next = _next.AddSeconds(1);
            return current;
        }
    }
}
