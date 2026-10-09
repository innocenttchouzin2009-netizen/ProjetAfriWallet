using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using MobileMoney.Production.Payout.Contracts;
using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Application;
using MobileMoney.Production.Payout.Funding.Contracts;
using MobileMoney.Production.Payout.Funding.Domain;
using MobileMoney.Production.Payout.Funding.Extensions;

internal static class ApiScenarios
{
    public static Task RouteIsStable()
    {
        AssertEqual(
            "/api/v1/mobile-money/payouts/funding/plan",
            MobileMoneyPayoutFundingEndpointExtensions.Route);

        return Task.CompletedTask;
    }

    public static async Task ReturnsFundingPlan()
    {
        var sources = new StubFundingSourceReader(
            new FundingSourceSnapshot(
                "wallet-main",
                FundingSourceType.Wallet,
                "EUR",
                true,
                5_000),
            new FundingSourceSnapshot(
                "apple-pay",
                FundingSourceType.ApplePay,
                "EUR",
                true,
                null));

        var result = await MobileMoneyPayoutFundingEndpointExtensions.HandleAsync(
            ValidRequest(),
            CreateService(sources),
            new DefaultHttpContext(),
            CancellationToken.None);

        var ok = result as Ok<MobileMoneyPayoutFundingPlanResponse>
            ?? throw new InvalidOperationException(
                "Expected HTTP 200 payout funding plan.");

        Assert(ok.Value is not null, "Funding plan response is required.");
        AssertEqual(10_000L, ok.Value!.RequiredAmountMinor);
        AssertEqual("EUR", ok.Value.CurrencyCode);
        AssertEqual(2, ok.Value.Allocations.Count);
        AssertEqual(10_000L, ok.Value.Allocations.Sum(x => x.AmountMinor));
    }

    public static async Task MapsInvalidRequest()
    {
        var request = ValidRequest() with
        {
            Allocations =
            [
                new MobileMoneyPayoutFundingAllocationRequest(
                    "wallet-main",
                    FundingSourceType.Wallet,
                    -1,
                    "EUR")
            ]
        };

        var result = await MobileMoneyPayoutFundingEndpointExtensions.HandleAsync(
            request,
            CreateService(new StubFundingSourceReader()),
            new DefaultHttpContext(),
            CancellationToken.None);

        AssertError(
            result,
            StatusCodes.Status400BadRequest,
            "PAYOUT_FUNDING_INVALID_REQUEST");
    }

    public static async Task MapsRejectedFunding()
    {
        var request = ValidRequest() with
        {
            RequiredAmountMinor = 11_000
        };

        var sources = new StubFundingSourceReader(
            new FundingSourceSnapshot(
                "wallet-main",
                FundingSourceType.Wallet,
                "EUR",
                true,
                5_000),
            new FundingSourceSnapshot(
                "apple-pay",
                FundingSourceType.ApplePay,
                "EUR",
                true,
                null));

        var result = await MobileMoneyPayoutFundingEndpointExtensions.HandleAsync(
            request,
            CreateService(sources),
            new DefaultHttpContext(),
            CancellationToken.None);

        AssertError(
            result,
            StatusCodes.Status422UnprocessableEntity,
            "PAYOUT_FUNDING_REJECTED");
    }

    private static PlanMobileMoneyPayoutFundingRequest ValidRequest() =>
        new(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            10_000,
            "EUR",
            [
                new MobileMoneyPayoutFundingAllocationRequest(
                    "wallet-main",
                    FundingSourceType.Wallet,
                    4_000,
                    "EUR"),
                new MobileMoneyPayoutFundingAllocationRequest(
                    "apple-pay",
                    FundingSourceType.ApplePay,
                    6_000,
                    "EUR")
            ],
            new DateTimeOffset(
                2026,
                10,
                9,
                21,
                0,
                0,
                TimeSpan.Zero));

    private static MobileMoneyPayoutFundingPlanningService CreateService(
        IMobileMoneyPayoutFundingSourceReader sources) =>
        new(
            sources,
            new StubFundingAttemptStore(),
            new MobileMoneyPayoutSplitFundingValidator());

    private static void AssertError(
        IResult result,
        int expectedStatusCode,
        string expectedCode)
    {
        var error = result as JsonHttpResult<MobileMoneyPayoutErrorResponse>
            ?? throw new InvalidOperationException(
                "Expected JSON payout funding error result.");

        AssertEqual(expectedStatusCode, error.StatusCode);
        Assert(error.Value is not null, "Error response is required.");
        AssertEqual(expectedCode, error.Value!.Code);
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

    private sealed class StubFundingSourceReader(
        params FundingSourceSnapshot[] snapshots)
        : IMobileMoneyPayoutFundingSourceReader
    {
        private readonly Dictionary<
            (string SourceId, FundingSourceType SourceType),
            FundingSourceSnapshot> _items =
            snapshots.ToDictionary(x => (x.SourceId, x.SourceType));

        public Task<FundingSourceSnapshot?> GetAsync(
            string sourceId,
            FundingSourceType sourceType,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.TryGetValue((sourceId, sourceType), out var source);
            return Task.FromResult(source);
        }
    }

    private sealed class StubFundingAttemptStore
        : IMobileMoneyPayoutFundingAttemptStore
    {
        private readonly List<FundingAttempt> _items = [];

        public Task SaveAsync(
            FundingAttempt attempt,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.Add(attempt);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FundingAttempt>> FindByCorrelationIdAsync(
            Guid correlationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<FundingAttempt> result =
                _items.Where(x => x.CorrelationId == correlationId).ToArray();

            return Task.FromResult(result);
        }
    }
}
