using AfriWallet.Transfer.Application.Funding;
using AfriWallet.Transfer.Domain.Funding;

await RunAsync("split funding plan creates one planned attempt per allocation", async () =>
{
    var correlationId = Guid.NewGuid();
    var requestedAt = new DateTimeOffset(2026, 10, 7, 19, 30, 0, TimeSpan.Zero);
    var wallet = FundingAllocation.Create("wallet-main", FundingSourceType.Wallet, 4_000, "eur");
    var applePay = FundingAllocation.Create("apple-pay", FundingSourceType.ApplePay, 6_000, "EUR");
    var sources = new FakeFundingSourceReader(
        new FundingSourceSnapshot("wallet-main", FundingSourceType.Wallet, "EUR", true, 5_000),
        new FundingSourceSnapshot("apple-pay", FundingSourceType.ApplePay, "EUR", true, null));
    var attempts = new FakeFundingAttemptStore();
    var service = CreateService(sources, attempts);

    var plan = await service.PlanAsync(
        new PlanTransferFundingCommand(
            correlationId,
            10_000,
            "eur",
            new[] { wallet, applePay },
            requestedAt));

    Assert(plan.CorrelationId == correlationId, "Correlation id mismatch.");
    Assert(plan.RequiredAmountMinor == 10_000, "Required amount mismatch.");
    Assert(plan.CurrencyCode == "EUR", "Currency must be normalized.");
    Assert(plan.Allocations.Count == 2, "Split plan must preserve both allocations.");
    Assert(attempts.Saved.Count == 2, "Each allocation must create one funding attempt.");
    Assert(attempts.Saved.All(x => x.Status == FundingAttemptStatus.Planned), "New attempts must be Planned.");
    Assert(attempts.Saved.Sum(x => x.Allocation.AmountMinor) == 10_000, "Attempt amounts must cover the plan exactly.");
    Assert(attempts.Saved.All(x => x.CorrelationId == correlationId), "Attempts must preserve the funding correlation.");
});

await RunAsync("wallet allocation above available balance is rejected before attempt persistence", async () =>
{
    var wallet = FundingAllocation.Create("wallet-main", FundingSourceType.Wallet, 5_001, "EUR");
    var sources = new FakeFundingSourceReader(
        new FundingSourceSnapshot("wallet-main", FundingSourceType.Wallet, "EUR", true, 5_000));
    var attempts = new FakeFundingAttemptStore();
    var service = CreateService(sources, attempts);

    await ExpectAsync<InvalidOperationException>(() => service.PlanAsync(
        new PlanTransferFundingCommand(
            Guid.NewGuid(),
            5_001,
            "EUR",
            new[] { wallet },
            DateTimeOffset.UtcNow)));

    Assert(attempts.Saved.Count == 0, "Invalid funding must not create attempts.");
});

await RunAsync("split allocations must sum exactly to required amount", async () =>
{
    var wallet = FundingAllocation.Create("wallet-main", FundingSourceType.Wallet, 3_000, "EUR");
    var card = FundingAllocation.Create("card-main", FundingSourceType.PaymentCard, 4_000, "EUR");
    var sources = new FakeFundingSourceReader(
        new FundingSourceSnapshot("wallet-main", FundingSourceType.Wallet, "EUR", true, 5_000),
        new FundingSourceSnapshot("card-main", FundingSourceType.PaymentCard, "EUR", true, null));
    var attempts = new FakeFundingAttemptStore();
    var service = CreateService(sources, attempts);

    await ExpectAsync<InvalidOperationException>(() => service.PlanAsync(
        new PlanTransferFundingCommand(
            Guid.NewGuid(),
            8_000,
            "EUR",
            new[] { wallet, card },
            DateTimeOffset.UtcNow)));

    Assert(attempts.Saved.Count == 0, "Mismatched split funding must not create attempts.");
});

await RunAsync("unavailable external funding source is rejected", async () =>
{
    var googlePay = FundingAllocation.Create("google-pay", FundingSourceType.GooglePay, 2_000, "EUR");
    var sources = new FakeFundingSourceReader(
        new FundingSourceSnapshot("google-pay", FundingSourceType.GooglePay, "EUR", false, null));
    var attempts = new FakeFundingAttemptStore();
    var service = CreateService(sources, attempts);

    await ExpectAsync<InvalidOperationException>(() => service.PlanAsync(
        new PlanTransferFundingCommand(
            Guid.NewGuid(),
            2_000,
            "EUR",
            new[] { googlePay },
            DateTimeOffset.UtcNow)));

    Assert(attempts.Saved.Count == 0, "Unavailable funding must not create attempts.");
});

await RunAsync("attempt lifecycle follows planned processing succeeded", () =>
{
    var createdAt = new DateTimeOffset(2026, 10, 7, 20, 0, 0, TimeSpan.Zero);
    var attempt = FundingAttempt.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        FundingAllocation.Create("card-main", FundingSourceType.PaymentCard, 1_500, "EUR"),
        createdAt);

    attempt.MarkProcessing(createdAt.AddSeconds(1));
    Assert(attempt.Status == FundingAttemptStatus.Processing, "Attempt must enter Processing.");
    Assert(attempt.UpdatedAtUtc == createdAt.AddSeconds(1), "Processing timestamp mismatch.");

    attempt.MarkSucceeded(createdAt.AddSeconds(2));
    Assert(attempt.Status == FundingAttemptStatus.Succeeded, "Attempt must enter Succeeded.");
    Assert(attempt.StatusReason is null, "Succeeded attempt must not carry a failure reason.");

    return Task.CompletedTask;
});

await RunAsync("attempt cannot skip processing before success", async () =>
{
    var now = new DateTimeOffset(2026, 10, 7, 20, 0, 0, TimeSpan.Zero);
    var attempt = FundingAttempt.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        FundingAllocation.Create("sepa-main", FundingSourceType.Sepa, 1_000, "EUR"),
        now);

    await ExpectAsync<InvalidOperationException>(() =>
    {
        attempt.MarkSucceeded(now.AddSeconds(1));
        return Task.CompletedTask;
    });
});

await RunAsync("failed and cancelled attempts retain normalized reason and are terminal", async () =>
{
    var now = new DateTimeOffset(2026, 10, 7, 20, 0, 0, TimeSpan.Zero);
    var failed = FundingAttempt.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        FundingAllocation.Create("card-main", FundingSourceType.PaymentCard, 1_000, "EUR"),
        now);

    failed.MarkProcessing(now.AddSeconds(1));
    failed.MarkFailed(now.AddSeconds(2), "  provider-declined  ");
    Assert(failed.Status == FundingAttemptStatus.Failed, "Attempt must enter Failed.");
    Assert(failed.StatusReason == "provider-declined", "Failure reason must be normalized.");

    await ExpectAsync<InvalidOperationException>(() =>
    {
        failed.Cancel(now.AddSeconds(3), "late-cancel");
        return Task.CompletedTask;
    });

    var cancelled = FundingAttempt.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        FundingAllocation.Create("wallet-main", FundingSourceType.Wallet, 500, "EUR"),
        now);

    cancelled.Cancel(now.AddSeconds(1), "  user-cancelled  ");
    Assert(cancelled.Status == FundingAttemptStatus.Cancelled, "Attempt must enter Cancelled.");
    Assert(cancelled.StatusReason == "user-cancelled", "Cancellation reason must be normalized.");
});

await RunAsync("planning propagates cancellation token to funding ports", async () =>
{
    using var cts = new CancellationTokenSource();
    var token = cts.Token;
    var wallet = FundingAllocation.Create("wallet-main", FundingSourceType.Wallet, 1_000, "EUR");
    var sources = new FakeFundingSourceReader(
        new FundingSourceSnapshot("wallet-main", FundingSourceType.Wallet, "EUR", true, 2_000));
    var attempts = new FakeFundingAttemptStore();
    var service = CreateService(sources, attempts);

    await service.PlanAsync(
        new PlanTransferFundingCommand(
            Guid.NewGuid(),
            1_000,
            "EUR",
            new[] { wallet },
            DateTimeOffset.UtcNow),
        token);

    Assert(sources.LastToken == token, "Cancellation token must reach source reader.");
    Assert(attempts.LastToken == token, "Cancellation token must reach attempt store.");
});

Console.WriteLine("Transfer funding scenarios passed.");

static TransferFundingPlanningService CreateService(
    IFundingSourceReader sources,
    IFundingAttemptStore attempts) =>
    new(sources, attempts, new SplitFundingValidator());

static async Task RunAsync(string name, Func<Task> action)
{
    await action();
    Console.WriteLine($"PASS: {name}");
}

static async Task ExpectAsync<T>(Func<Task> action) where T : Exception
{
    try
    {
        await action();
    }
    catch (T)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

sealed class FakeFundingSourceReader(params FundingSourceSnapshot[] snapshots) : IFundingSourceReader
{
    private readonly Dictionary<(string SourceId, FundingSourceType SourceType), FundingSourceSnapshot> items =
        snapshots.ToDictionary(x => (x.SourceId, x.SourceType));

    public CancellationToken LastToken { get; private set; }

    public Task<FundingSourceSnapshot?> GetAsync(
        string sourceId,
        FundingSourceType sourceType,
        CancellationToken cancellationToken = default)
    {
        LastToken = cancellationToken;
        items.TryGetValue((sourceId, sourceType), out var source);
        return Task.FromResult(source);
    }
}

sealed class FakeFundingAttemptStore : IFundingAttemptStore
{
    public List<FundingAttempt> Saved { get; } = new();
    public CancellationToken LastToken { get; private set; }

    public Task SaveAsync(
        FundingAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        LastToken = cancellationToken;
        Saved.Add(attempt);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<FundingAttempt>> FindByCorrelationIdAsync(
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        LastToken = cancellationToken;
        IReadOnlyList<FundingAttempt> result =
            Saved.Where(x => x.CorrelationId == correlationId).ToArray();
        return Task.FromResult(result);
    }
}
