using MobileMoney.Production.Payout.Domain;
using MobileMoney.Production.Payout.Quote.Abstractions;
using MobileMoney.Production.Payout.Quote.Domain;
using MobileMoney.Production.Payout.Quote.Infrastructure;

internal static class RuntimeFeePolicyScenarios
{
    public static async Task SelectsTierAtBoundaries()
    {
        var corridor = Corridor();
        var policy = new ConfiguredMobileMoneyPayoutFeePolicy(
            new[]
            {
                Tier(corridor, 1, 10_000, ("SERVICE_FEE", 125L)),
                Tier(corridor, 10_000, null, ("SERVICE_FEE", 200L), ("RAIL_FEE", 50L))
            });

        var lower = await policy.CalculateAsync(
            new MobileMoneyPayoutFeeContext(corridor, 9_999));
        var upper = await policy.CalculateAsync(
            new MobileMoneyPayoutFeeContext(corridor, 10_000));

        AssertEqual(1, lower.Count);
        AssertEqual(125L, lower[0].AmountMinor);
        AssertEqual("EUR", lower[0].Currency);

        AssertEqual(2, upper.Count);
        AssertEqual(200L, upper[0].AmountMinor);
        AssertEqual(50L, upper[1].AmountMinor);
    }

    public static async Task SupportsExplicitZeroFeeTier()
    {
        var corridor = Corridor();
        var policy = new ConfiguredMobileMoneyPayoutFeePolicy(
            new[]
            {
                new ConfiguredMobileMoneyPayoutFeeTier(
                    corridor,
                    1,
                    null,
                    Array.Empty<MobileMoneyPayoutFee>())
            });

        var fees = await policy.CalculateAsync(
            new MobileMoneyPayoutFeeContext(corridor, 5_000));

        AssertEqual(0, fees.Count);
    }

    public static async Task FailsClosedWhenCorridorIsUnconfigured()
    {
        var policy = new ConfiguredMobileMoneyPayoutFeePolicy(
            new[] { Tier(Corridor(), 1, null, ("SERVICE_FEE", 125L)) });

        await AssertThrowsAsync<KeyNotFoundException>(() =>
            policy.CalculateAsync(
                    new MobileMoneyPayoutFeeContext(
                        new MobileMoneyPayoutCorridor(
                            "FR", "EUR", "CM", "XAF", "MTN-CM"),
                        5_000))
                .AsTask());
    }

    public static async Task FailsClosedWhenAmountFallsInGap()
    {
        var corridor = Corridor();
        var policy = new ConfiguredMobileMoneyPayoutFeePolicy(
            new[]
            {
                Tier(corridor, 1, 5_000, ("SERVICE_FEE", 100L)),
                Tier(corridor, 10_000, null, ("SERVICE_FEE", 200L))
            });

        await AssertThrowsAsync<KeyNotFoundException>(() =>
            policy.CalculateAsync(
                    new MobileMoneyPayoutFeeContext(corridor, 7_500))
                .AsTask());
    }

    public static Task RejectsOverlappingTiers()
    {
        var corridor = Corridor();

        AssertThrows<ArgumentException>(() =>
            new ConfiguredMobileMoneyPayoutFeePolicy(
                new[]
                {
                    Tier(corridor, 1, 10_000, ("SERVICE_FEE", 100L)),
                    Tier(corridor, 9_999, null, ("SERVICE_FEE", 200L))
                }));

        return Task.CompletedTask;
    }

    public static Task RejectsFeeCurrencyMismatch()
    {
        var corridor = Corridor();

        AssertThrows<ArgumentException>(() =>
            new ConfiguredMobileMoneyPayoutFeeTier(
                corridor,
                1,
                null,
                new[]
                {
                    new MobileMoneyPayoutFee("SERVICE_FEE", 125, "USD")
                }));

        return Task.CompletedTask;
    }

    public static Task RejectsDuplicateFeeCodes()
    {
        var corridor = Corridor();

        AssertThrows<ArgumentException>(() =>
            new ConfiguredMobileMoneyPayoutFeeTier(
                corridor,
                1,
                null,
                new[]
                {
                    new MobileMoneyPayoutFee("SERVICE_FEE", 100, "EUR"),
                    new MobileMoneyPayoutFee(" service_fee ", 25, "EUR")
                }));

        return Task.CompletedTask;
    }

    public static async Task HonorsCancellation()
    {
        var corridor = Corridor();
        var policy = new ConfiguredMobileMoneyPayoutFeePolicy(
            new[] { Tier(corridor, 1, null, ("SERVICE_FEE", 125L)) });

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await AssertThrowsAsync<OperationCanceledException>(() =>
            policy.CalculateAsync(
                    new MobileMoneyPayoutFeeContext(corridor, 5_000),
                    cancellation.Token)
                .AsTask());
    }

    private static ConfiguredMobileMoneyPayoutFeeTier Tier(
        MobileMoneyPayoutCorridor corridor,
        long minimumSourceAmountMinor,
        long? maximumSourceAmountMinorExclusive,
        params (string Code, long AmountMinor)[] fees) =>
        new(
            corridor,
            minimumSourceAmountMinor,
            maximumSourceAmountMinorExclusive,
            fees.Select(fee =>
                new MobileMoneyPayoutFee(
                    fee.Code,
                    fee.AmountMinor,
                    corridor.SourceCurrency)));

    private static MobileMoneyPayoutCorridor Corridor() =>
        new("DE", "EUR", "CM", "XAF", "MTN-CM");

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"Expected '{expected}', got '{actual}'.");
        }
    }

    private static void AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Expected exception {typeof(TException).Name}.");
    }

    private static async Task AssertThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Expected exception {typeof(TException).Name}.");
    }
}
