using MobileMoney.Production.Payout.Domain;
using MobileMoney.Production.Payout.Execution.Abstractions;
using MobileMoney.Production.Payout.Execution.Application;
using MobileMoney.Production.Payout.Execution.Domain;

internal static class PayoutExecutionResolutionScenarios
{
    public static void RunAll() =>
        RunAllAsync().GetAwaiter().GetResult();

    private static async Task RunAllAsync()
    {
        var scenarios = new (string Name, Func<Task> Run)[]
        {
            ("new execution resolves quote and reserves idempotency key", NewExecutionResolvesAndReserves),
            ("missing quote rejects before idempotency reservation", MissingQuoteRejectsBeforeReservation),
            ("matching replay bypasses quote lookup even after quote expiry", MatchingReplayBypassesQuoteLookup),
            ("same idempotency key with different intent conflicts", ExistingKeyDifferentIntentConflicts),
            ("matching reservation collision becomes deterministic replay", MatchingCollisionBecomesReplay),
            ("conflicting reservation collision is rejected", ConflictingCollisionIsRejected)
        };

        foreach (var scenario in scenarios)
        {
            await scenario.Run();
            Console.WriteLine($"PASS: {scenario.Name}");
        }

        Console.WriteLine(
            $"MobileMoney payout execution resolution scenarios: {scenarios.Length}/{scenarios.Length} passed.");
    }

    private static async Task NewExecutionResolvesAndReserves()
    {
        var quote = Quote();
        var intent = Intent(
            quote.QuoteId,
            "+237690000001",
            "MTN-CM",
            "execute-new");

        var quoteReader = new FakeQuoteReader(quote);
        var idempotencyStore = new FakeIdempotencyStore();
        var resolver = new MobileMoneyPayoutExecutionResolver(
            quoteReader,
            idempotencyStore);

        var resolution = await resolver.ResolveAsync(
            intent,
            quote.CreatedAtUtc.AddMinutes(1));

        Assert(resolution.ShouldExecute, "First reservation must be executable.");
        Assert(!resolution.IsReplay, "First reservation cannot be a replay.");
        Assert(resolution.Binding is not null, "New execution must carry a resolved binding.");
        AssertEqual(quote.QuoteId, resolution.Binding!.Quote.QuoteId);
        Assert(resolution.IdempotencyEntry.Matches(intent),
            "Reserved idempotency entry must match the execution intent.");
        AssertEqual(1, quoteReader.FindCount);
        AssertEqual(1, idempotencyStore.Count);
    }

    private static async Task MissingQuoteRejectsBeforeReservation()
    {
        var quoteId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var intent = Intent(
            quoteId,
            "+237690000001",
            "MTN-CM",
            "missing-quote");

        var quoteReader = new FakeQuoteReader();
        var idempotencyStore = new FakeIdempotencyStore();
        var resolver = new MobileMoneyPayoutExecutionResolver(
            quoteReader,
            idempotencyStore);

        var exception = await AssertThrowsAsync<MobileMoneyPayoutExecutionException>(
            () => resolver.ResolveAsync(
                intent,
                Utc(2026, 10, 9, 9, 1)));

        AssertEqual(
            MobileMoneyPayoutExecutionErrorCodes.QuoteNotFound,
            exception.Code);
        AssertEqual(1, quoteReader.FindCount);
        AssertEqual(0, idempotencyStore.Count);
    }

    private static async Task MatchingReplayBypassesQuoteLookup()
    {
        var quote = Quote();
        var intent = Intent(
            quote.QuoteId,
            "+237690000001",
            "MTN-CM",
            "replay-key");
        var existing = Entry(intent, quote.CreatedAtUtc.AddMinutes(1));

        var quoteReader = new FakeQuoteReader();
        var idempotencyStore = new FakeIdempotencyStore(existing);
        var resolver = new MobileMoneyPayoutExecutionResolver(
            quoteReader,
            idempotencyStore);

        var resolution = await resolver.ResolveAsync(
            intent,
            quote.ExpiresAtUtc.AddHours(1));

        Assert(resolution.IsReplay, "Matching existing reservation must replay.");
        Assert(!resolution.ShouldExecute, "Replay must never request provider execution.");
        Assert(resolution.Binding is null, "Replay must not require a fresh quote binding.");
        AssertEqual(existing, resolution.IdempotencyEntry);
        AssertEqual(0, quoteReader.FindCount);
        AssertEqual(1, idempotencyStore.Count);
    }

    private static async Task ExistingKeyDifferentIntentConflicts()
    {
        var quote = Quote();
        var storedIntent = Intent(
            quote.QuoteId,
            "+237690000001",
            "MTN-CM",
            "collision-key");
        var conflictingIntent = Intent(
            quote.QuoteId,
            "+237690000002",
            "MTN-CM",
            "collision-key");

        var quoteReader = new FakeQuoteReader(quote);
        var idempotencyStore = new FakeIdempotencyStore(
            Entry(storedIntent, quote.CreatedAtUtc));
        var resolver = new MobileMoneyPayoutExecutionResolver(
            quoteReader,
            idempotencyStore);

        var exception = await AssertThrowsAsync<MobileMoneyPayoutExecutionException>(
            () => resolver.ResolveAsync(
                conflictingIntent,
                quote.CreatedAtUtc.AddMinutes(1)));

        AssertEqual(
            MobileMoneyPayoutExecutionErrorCodes.IdempotencyConflict,
            exception.Code);
        AssertEqual(0, quoteReader.FindCount);
        AssertEqual(1, idempotencyStore.Count);
    }

    private static async Task MatchingCollisionBecomesReplay()
    {
        var quote = Quote();
        var intent = Intent(
            quote.QuoteId,
            "+237690000001",
            "MTN-CM",
            "race-replay");
        var competing = Entry(intent, quote.CreatedAtUtc.AddSeconds(30));

        var quoteReader = new FakeQuoteReader(quote);
        var idempotencyStore = new FakeIdempotencyStore
        {
            CompetingEntryOnNextCreate = competing
        };
        var resolver = new MobileMoneyPayoutExecutionResolver(
            quoteReader,
            idempotencyStore);

        var resolution = await resolver.ResolveAsync(
            intent,
            quote.CreatedAtUtc.AddMinutes(1));

        Assert(resolution.IsReplay,
            "Losing a matching reservation race must resolve as replay.");
        Assert(!resolution.ShouldExecute,
            "Race loser must never proceed to provider execution.");
        AssertEqual(competing, resolution.IdempotencyEntry);
        AssertEqual(1, quoteReader.FindCount);
        AssertEqual(1, idempotencyStore.Count);
    }

    private static async Task ConflictingCollisionIsRejected()
    {
        var quote = Quote();
        var intent = Intent(
            quote.QuoteId,
            "+237690000001",
            "MTN-CM",
            "race-conflict");
        var conflicting = Intent(
            quote.QuoteId,
            "+237690000002",
            "MTN-CM",
            "race-conflict");

        var quoteReader = new FakeQuoteReader(quote);
        var idempotencyStore = new FakeIdempotencyStore
        {
            CompetingEntryOnNextCreate = Entry(
                conflicting,
                quote.CreatedAtUtc.AddSeconds(30))
        };
        var resolver = new MobileMoneyPayoutExecutionResolver(
            quoteReader,
            idempotencyStore);

        var exception = await AssertThrowsAsync<MobileMoneyPayoutExecutionException>(
            () => resolver.ResolveAsync(
                intent,
                quote.CreatedAtUtc.AddMinutes(1)));

        AssertEqual(
            MobileMoneyPayoutExecutionErrorCodes.IdempotencyConflict,
            exception.Code);
        AssertEqual(1, quoteReader.FindCount);
        AssertEqual(1, idempotencyStore.Count);
    }

    private static MobileMoneyPayoutExecutionIdempotencyEntry Entry(
        MobileMoneyPayoutExecutionIntent intent,
        DateTimeOffset createdAtUtc) =>
        new(
            intent.IdempotencyKey,
            intent.Fingerprint,
            intent.QuoteId,
            null,
            createdAtUtc);

    private static MobileMoneyPayoutExecutionIntent Intent(
        Guid quoteId,
        string msisdn,
        string operatorCode,
        string key) =>
        MobileMoneyPayoutExecutionIntent.Create(
            quoteId,
            "wallet-001",
            new MobileMoneyBeneficiary(
                msisdn,
                "CM",
                operatorCode,
                "Ada"),
            key);

    private static MobileMoneyPayoutExecutionQuote Quote()
    {
        var created = Utc(2026, 10, 9, 9, 0);

        return new MobileMoneyPayoutExecutionQuote(
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            new MobileMoneyPayoutCorridor(
                "DE",
                "EUR",
                "CM",
                "XAF",
                "MTN-CM"),
            25_000,
            15_000_000,
            600m,
            500,
            25_500,
            created,
            created.AddMinutes(10));
    }

    private static DateTimeOffset Utc(
        int year,
        int month,
        int day,
        int hour,
        int minute) =>
        new(year, month, day, hour, minute, 0, TimeSpan.Zero);

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

    private static async Task<TException> AssertThrowsAsync<TException>(
        Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException exception)
        {
            return exception;
        }

        throw new InvalidOperationException(
            $"Expected exception {typeof(TException).Name}.");
    }

    private sealed class FakeQuoteReader : IMobileMoneyPayoutExecutionQuoteReader
    {
        private readonly IReadOnlyDictionary<Guid, MobileMoneyPayoutExecutionQuote> _quotes;

        public FakeQuoteReader(params MobileMoneyPayoutExecutionQuote[] quotes)
        {
            _quotes = quotes.ToDictionary(quote => quote.QuoteId);
        }

        public int FindCount { get; private set; }

        public Task<MobileMoneyPayoutExecutionQuote?> FindAsync(
            Guid quoteId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FindCount++;
            _quotes.TryGetValue(quoteId, out var quote);
            return Task.FromResult(quote);
        }
    }

    private sealed class FakeIdempotencyStore
        : IMobileMoneyPayoutExecutionIdempotencyStore
    {
        private readonly Dictionary<string, MobileMoneyPayoutExecutionIdempotencyEntry> _entries =
            new(StringComparer.Ordinal);

        public FakeIdempotencyStore(
            params MobileMoneyPayoutExecutionIdempotencyEntry[] entries)
        {
            foreach (var entry in entries)
                _entries.Add(entry.IdempotencyKey, entry);
        }

        public int Count => _entries.Count;

        public MobileMoneyPayoutExecutionIdempotencyEntry? CompetingEntryOnNextCreate
        {
            get;
            init;
        }

        public Task<MobileMoneyPayoutExecutionIdempotencyEntry?> FindAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _entries.TryGetValue(idempotencyKey, out var entry);
            return Task.FromResult(entry);
        }

        public Task<bool> TryCreateAsync(
            MobileMoneyPayoutExecutionIdempotencyEntry entry,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (CompetingEntryOnNextCreate is not null)
            {
                _entries[entry.IdempotencyKey] = CompetingEntryOnNextCreate;
                return Task.FromResult(false);
            }

            return Task.FromResult(
                _entries.TryAdd(entry.IdempotencyKey, entry));
        }
    }
}
