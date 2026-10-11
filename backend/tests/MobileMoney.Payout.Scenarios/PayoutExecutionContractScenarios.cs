using MobileMoney.Production.Payout.Contracts;
using MobileMoney.Production.Payout.Domain;
using MobileMoney.Production.Payout.Execution.Contracts;
using MobileMoney.Production.Payout.Execution.Domain;

internal static class PayoutExecutionContractScenarios
{
    public static void RunAll()
    {
        var scenarios = new (string Name, Action Run)[]
        {
            ("execution contract binds quote instead of resubmitting money terms", ContractSurface),
            ("execution intent normalizes and fingerprints deterministically", DeterministicFingerprint),
            ("materially different execution intent changes fingerprint", DifferentIntentChangesFingerprint),
            ("matching quote and beneficiary bind before expiry", MatchingBinding),
            ("quote expires exactly at expiry timestamp", ExactExpiryIsRejected),
            ("beneficiary corridor mismatch rejects quote binding", CorridorMismatchIsRejected),
            ("idempotency entry distinguishes replay from conflicting intent", IdempotencyEntryMatchesIntent)
        };

        foreach (var scenario in scenarios)
        {
            scenario.Run();
            Console.WriteLine($"PASS: {scenario.Name}");
        }

        Console.WriteLine(
            $"MobileMoney payout execution contract scenarios: {scenarios.Length}/{scenarios.Length} passed.");
    }

    private static void ContractSurface()
    {
        var quoteId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var request = new ExecuteMobileMoneyPayoutRequest(
            quoteId,
            "wallet-001",
            new MobileMoneyBeneficiaryRequest(
                "+237690000001",
                "CM",
                "MTN-CM",
                "Ada"),
            "execute-key-001");

        AssertEqual(quoteId, request.QuoteId);
        AssertEqual("wallet-001", request.SourceWalletId);
        AssertEqual("+237690000001", request.Beneficiary.Msisdn);
        AssertEqual("execute-key-001", request.IdempotencyKey);
    }

    private static void DeterministicFingerprint()
    {
        var quoteId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var first = MobileMoneyPayoutExecutionIntent.Create(
            quoteId,
            " wallet-001 ",
            new MobileMoneyBeneficiary(
                " +237690000001 ",
                " cm ",
                " mtn-cm ",
                "Ada"),
            " key-001 ");

        var second = MobileMoneyPayoutExecutionIntent.Create(
            quoteId,
            "wallet-001",
            new MobileMoneyBeneficiary(
                "+237690000001",
                "CM",
                "MTN-CM",
                "Different display name"),
            "key-001");

        AssertEqual("wallet-001", first.SourceWalletId);
        AssertEqual("key-001", first.IdempotencyKey);
        AssertEqual(first.Fingerprint, second.Fingerprint);
        AssertEqual(64, first.Fingerprint.Length);
    }

    private static void DifferentIntentChangesFingerprint()
    {
        var quoteId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var first = Intent(quoteId, "+237690000001", "MTN-CM", "key-001");
        var differentQuote = Intent(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "+237690000001",
            "MTN-CM",
            "key-001");
        var differentBeneficiary = Intent(
            quoteId,
            "+237690000002",
            "MTN-CM",
            "key-001");

        Assert(first.Fingerprint != differentQuote.Fingerprint,
            "Changing the quote must change the fingerprint.");
        Assert(first.Fingerprint != differentBeneficiary.Fingerprint,
            "Changing the beneficiary must change the fingerprint.");
    }

    private static void MatchingBinding()
    {
        var quote = Quote();
        var intent = Intent(quote.QuoteId, "+237690000001", "MTN-CM", "key-binding");

        var binding = MobileMoneyPayoutExecutionBinding.Create(
            quote,
            intent,
            quote.CreatedAtUtc.AddMinutes(1));

        AssertEqual(quote.QuoteId, binding.Quote.QuoteId);
        AssertEqual(intent.Fingerprint, binding.Intent.Fingerprint);
        AssertEqual(25_000L, binding.Quote.SourceAmountMinor);
        AssertEqual(15_000_000L, binding.Quote.DestinationAmountMinor);
        AssertEqual(25_500L, binding.Quote.TotalSourceDebitMinor);
    }

    private static void ExactExpiryIsRejected()
    {
        var quote = Quote();
        var intent = Intent(quote.QuoteId, "+237690000001", "MTN-CM", "key-expiry");

        var exception = AssertThrows<MobileMoneyPayoutExecutionException>(() =>
            MobileMoneyPayoutExecutionBinding.Create(
                quote,
                intent,
                quote.ExpiresAtUtc));

        AssertEqual(
            MobileMoneyPayoutExecutionErrorCodes.QuoteExpired,
            exception.Code);
    }

    private static void CorridorMismatchIsRejected()
    {
        var quote = Quote();
        var intent = Intent(
            quote.QuoteId,
            "+237690000001",
            "ORANGE-CM",
            "key-mismatch");

        var exception = AssertThrows<MobileMoneyPayoutExecutionException>(() =>
            MobileMoneyPayoutExecutionBinding.Create(
                quote,
                intent,
                quote.CreatedAtUtc.AddMinutes(1)));

        AssertEqual(
            MobileMoneyPayoutExecutionErrorCodes.QuoteBindingMismatch,
            exception.Code);
    }

    private static void IdempotencyEntryMatchesIntent()
    {
        var quote = Quote();
        var intent = Intent(
            quote.QuoteId,
            "+237690000001",
            "MTN-CM",
            "key-idempotency");

        var entry = new MobileMoneyPayoutExecutionIdempotencyEntry(
            intent.IdempotencyKey,
            intent.Fingerprint,
            intent.QuoteId,
            null,
            quote.CreatedAtUtc);

        Assert(entry.Matches(intent), "Same execution intent must be replay-compatible.");

        var conflicting = Intent(
            quote.QuoteId,
            "+237690000002",
            "MTN-CM",
            "key-idempotency");

        Assert(!entry.Matches(conflicting),
            "Same key with a different execution fingerprint must conflict.");
    }

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
        var created = new DateTimeOffset(
            2026, 10, 9, 9, 0, 0, TimeSpan.Zero);

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

    private static TException AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }

        throw new InvalidOperationException(
            $"Expected exception {typeof(TException).Name}.");
    }
}
