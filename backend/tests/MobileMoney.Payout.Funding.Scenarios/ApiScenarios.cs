using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using MobileMoney.Production.Authentication;
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
        var result = await MobileMoneyPayoutFundingEndpointExtensions.HandleAsync(
            ValidRequest(),
            CreateService(new StubFundingSourceReader()),
            AuthenticatedContext(),
            CancellationToken.None);

        var ok = result as Ok<MobileMoneyPayoutFundingPlanResponse>
            ?? throw new InvalidOperationException(
                "Expected HTTP 200 payout funding plan.");

        Assert(ok.Value is not null, "Funding plan response is required.");
        AssertEqual(10_000L, ok.Value!.RequiredAmountMinor);
        AssertEqual("EUR", ok.Value.CurrencyCode);
        AssertEqual(1, ok.Value.Allocations.Count);
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
            AuthenticatedContext(),
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

        var result = await MobileMoneyPayoutFundingEndpointExtensions.HandleAsync(
            request,
            CreateService(new StubFundingSourceReader()),
            AuthenticatedContext(),
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
                    "33333333-3333-4333-8333-333333333333",
                    FundingSourceType.Wallet,
                    10_000,
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
            new MobileMoneyPayoutSplitFundingValidator(),
            new StubAuthoritativeWalletSourceReader());

    private static DefaultHttpContext AuthenticatedContext()
    {
        var context = new DefaultHttpContext();
        context.Items[AfWalValidatedSession.HttpContextItemKey] =
            new AfWalValidatedSession(
                Guid.Parse("11111111-1111-4111-8111-111111111111"),
                Guid.Parse("22222222-2222-4222-8222-222222222222"),
                "scenario-device",
                DateTimeOffset.UtcNow.AddHours(1),
                1);
        return context;
    }

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

    private sealed class StubAuthoritativeWalletSourceReader
        : IMobileMoneyPayoutAuthoritativeWalletSourceReader
    {
        public Task<FundingSourceSnapshot?> GetAsync(
            Guid ownerUserId,
            string sourceId,
            string currencyCode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (sourceId != "33333333-3333-4333-8333-333333333333"
                || currencyCode != "EUR")
            {
                return Task.FromResult<FundingSourceSnapshot?>(null);
            }

            return Task.FromResult<FundingSourceSnapshot?>(
                new FundingSourceSnapshot(
                    sourceId,
                    FundingSourceType.Wallet,
                    "EUR",
                    true,
                    10_000));
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

        private readonly Dictionary<Guid, Guid> _owners = [];

        public async Task<IReadOnlyList<FundingAttempt>> SavePlanForOwnerAsync(
            Guid ownerUserId,
            IReadOnlyList<FundingAttempt> attempts,
            CancellationToken cancellationToken = default)
        {
            _owners[attempts[0].CorrelationId] = ownerUserId;
            return await ((IMobileMoneyPayoutFundingAttemptStore)this)
                .SavePlanAsync(attempts, cancellationToken);
        }

        public Task<Guid?> FindOwnerUserIdAsync(
            Guid correlationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _owners.TryGetValue(correlationId, out var owner)
                    ? (Guid?)owner
                    : null);

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
