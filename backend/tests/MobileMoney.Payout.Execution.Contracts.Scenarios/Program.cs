using MobileMoney.Production.Payout.Execution.Abstractions;
using MobileMoney.Production.Payout.Execution.Domain;
using MobileMoney.Production.Payout.Funding.Domain;

await RunAsync("idempotency key is required and normalized", () =>
{
    var key = MobileMoneyPayoutExecutionIdempotencyKey.Create("  payout-exec-1  ");
    Assert(key.Value == "payout-exec-1", "Idempotency key must be trimmed.");
    return Task.CompletedTask;
});

await RunAsync("blank idempotency key is rejected", async () =>
{
    await ExpectAsync<ArgumentException>(() =>
    {
        _ = MobileMoneyPayoutExecutionIdempotencyKey.Create("   ");
        return Task.CompletedTask;
    });
});

await RunAsync("funding binding requires exact coverage", async () =>
{
    var allocations = new[]
    {
        MobileMoneyPayoutExecutionFundingAllocation.Create(
            "wallet-main",
            FundingSourceType.Wallet,
            4_000,
            "EUR"),
        MobileMoneyPayoutExecutionFundingAllocation.Create(
            "apple-pay",
            FundingSourceType.ApplePay,
            5_000,
            "EUR"),
    };

    await ExpectAsync<ArgumentException>(() =>
    {
        _ = MobileMoneyPayoutExecutionFundingBinding.Create(
            Guid.NewGuid(),
            10_000,
            "EUR",
            allocations);
        return Task.CompletedTask;
    });
});

await RunAsync("execution binding preserves quote funding and idempotency contract", async () =>
{
    var now = new DateTimeOffset(2026, 10, 11, 15, 0, 0, TimeSpan.Zero);
    var quote = CreateQuote(now.AddMinutes(5));
    var funding = CreateFunding(quote.TotalSourceDebitMinor, quote.SourceCurrency);
    var key = MobileMoneyPayoutExecutionIdempotencyKey.Create("payout-exec-2");

    var binding = MobileMoneyPayoutExecutionBinding.Create(
        quote,
        funding,
        key,
        now);

    Assert(binding.BindingId != Guid.Empty, "Binding id must be assigned.");
    Assert(binding.Quote.QuoteId == quote.QuoteId, "Quote binding mismatch.");
    Assert(
        binding.Funding.CorrelationId == funding.CorrelationId,
        "Funding binding mismatch.");
    Assert(
        binding.IdempotencyKey.Value == "payout-exec-2",
        "Idempotency binding mismatch.");

    var store = new InMemoryBindingStore();
    await store.SaveAsync(binding);
    var replay = await store.FindByIdempotencyKeyAsync(key);

    Assert(replay == binding, "Idempotency lookup must return the same binding.");
});

await RunAsync("execution binding rejects expired quote", async () =>
{
    var now = new DateTimeOffset(2026, 10, 11, 15, 0, 0, TimeSpan.Zero);
    var quote = CreateQuote(now);
    var funding = CreateFunding(quote.TotalSourceDebitMinor, quote.SourceCurrency);

    await ExpectAsync<InvalidOperationException>(() =>
    {
        _ = MobileMoneyPayoutExecutionBinding.Create(
            quote,
            funding,
            MobileMoneyPayoutExecutionIdempotencyKey.Create("payout-expired"),
            now);
        return Task.CompletedTask;
    });
});

await RunAsync("execution binding rejects funding currency mismatch", async () =>
{
    var now = new DateTimeOffset(2026, 10, 11, 15, 0, 0, TimeSpan.Zero);
    var quote = CreateQuote(now.AddMinutes(5));
    var funding = CreateFunding(quote.TotalSourceDebitMinor, "USD");

    await ExpectAsync<InvalidOperationException>(() =>
    {
        _ = MobileMoneyPayoutExecutionBinding.Create(
            quote,
            funding,
            MobileMoneyPayoutExecutionIdempotencyKey.Create("payout-currency"),
            now);
        return Task.CompletedTask;
    });
});

await RunAsync("execution binding rejects funding amount mismatch", async () =>
{
    var now = new DateTimeOffset(2026, 10, 11, 15, 0, 0, TimeSpan.Zero);
    var quote = CreateQuote(now.AddMinutes(5));
    var funding = CreateFunding(quote.TotalSourceDebitMinor + 1, quote.SourceCurrency);

    await ExpectAsync<InvalidOperationException>(() =>
    {
        _ = MobileMoneyPayoutExecutionBinding.Create(
            quote,
            funding,
            MobileMoneyPayoutExecutionIdempotencyKey.Create("payout-amount"),
            now);
        return Task.CompletedTask;
    });
});

await BindingAdapterScenarios.RunAsync();

Console.WriteLine("Mobile Money payout execution contract scenarios passed.");

static MobileMoneyPayoutExecutionQuoteBinding CreateQuote(DateTimeOffset expiresAtUtc) =>
    MobileMoneyPayoutExecutionQuoteBinding.Create(
        Guid.NewGuid(),
        "eur",
        9_500,
        500,
        10_000,
        "xaf",
        6_200_000,
        expiresAtUtc);

static MobileMoneyPayoutExecutionFundingBinding CreateFunding(
    long requiredAmountMinor,
    string currencyCode) =>
    MobileMoneyPayoutExecutionFundingBinding.Create(
        Guid.NewGuid(),
        requiredAmountMinor,
        currencyCode,
        new[]
        {
            MobileMoneyPayoutExecutionFundingAllocation.Create(
                "wallet-main",
                FundingSourceType.Wallet,
                requiredAmountMinor,
                currencyCode),
        });

static async Task RunAsync(string name, Func<Task> action)
{
    await action();
    Console.WriteLine($"PASS: {name}");
}

static async Task ExpectAsync<T>(Func<Task> action)
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

    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

sealed class InMemoryBindingStore : IMobileMoneyPayoutExecutionBindingStore
{
    private readonly Dictionary<string, MobileMoneyPayoutExecutionBinding> _items =
        new(StringComparer.Ordinal);

    public Task<MobileMoneyPayoutExecutionBinding?> FindByIdempotencyKeyAsync(
        MobileMoneyPayoutExecutionIdempotencyKey idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        _items.TryGetValue(idempotencyKey.Value, out var binding);
        return Task.FromResult(binding);
    }

    public Task SaveAsync(
        MobileMoneyPayoutExecutionBinding binding,
        CancellationToken cancellationToken = default)
    {
        _items[binding.IdempotencyKey.Value] = binding;
        return Task.CompletedTask;
    }
}
