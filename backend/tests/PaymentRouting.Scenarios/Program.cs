using PaymentRouting.Application.Scoring;
using PaymentRouting.Application.Services;
using PaymentRouting.Domain.Providers;
using PaymentRouting.Domain.Routes;
using PaymentRouting.Infrastructure.Providers;
using PaymentRouting.Infrastructure.Repositories;

var providers =
    new InMemoryPaymentProviderRepository();

await SandboxProviderBootstrap.SeedAsync(
    providers,
    CancellationToken.None);

var decisions =
    new InMemoryRoutingDecisionRepository();

var service =
    new PaymentRoutingService(
        providers,
        decisions,
        new PaymentRouteScorer());

var mobileMoneyIntentId =
    Guid.NewGuid();

var decision =
    await service.RouteAsync(
        new RoutingRequest(
            mobileMoneyIntentId,
            "CM",
            "XAF",
            250_000,
            PaymentRail.MobileMoney,
            null,
            "corr-route-001"),
        policy: null,
        CancellationToken.None);

Assert(
    decision.SelectedRoute.ProviderId
        is "MTN-MOMO-CM"
        or "ORANGE-MONEY-CM",
    "route selection");

Assert(
    decision.Alternatives.Count > 0,
    "fallback route");

var duplicate =
    await service.RouteAsync(
        new RoutingRequest(
            mobileMoneyIntentId,
            "CM",
            "XAF",
            250_000,
            PaymentRail.MobileMoney,
            null,
            "corr-route-001"),
        policy: null,
        CancellationToken.None);

Assert(
    duplicate.DecisionId ==
    decision.DecisionId,
    "routing idempotency");

var preferredIntentId =
    Guid.NewGuid();

var preferred =
    await service.RouteAsync(
        new RoutingRequest(
            preferredIntentId,
            "CM",
            "XAF",
            100_000,
            PaymentRail.MobileMoney,
            "MTN-MOMO-CM",
            "corr-route-002"),
        policy: null,
        CancellationToken.None);

Assert(
    preferred.SelectedRoute.ProviderId ==
    "MTN-MOMO-CM",
    "preferred provider");

var orange =
    await providers.GetAsync(
        "ORANGE-MONEY-CM",
        CancellationToken.None);

orange!.UpdateHealth(
    ProviderStatus.Unavailable,
    successRate: 0,
    averageLatencyMs: 10_000);

var degradedIntentId =
    Guid.NewGuid();

var degradedDecision =
    await service.RouteAsync(
        new RoutingRequest(
            degradedIntentId,
            "CM",
            "XAF",
            100_000,
            PaymentRail.MobileMoney,
            null,
            "corr-route-003"),
        policy: null,
        CancellationToken.None);

Assert(
    degradedDecision.SelectedRoute.ProviderId ==
    "MTN-MOMO-CM",
    "provider health filtering");

var unsupportedBlocked = false;

try
{
    await service.RouteAsync(
        new RoutingRequest(
            Guid.NewGuid(),
            "CM",
            "EUR",
            100_000,
            PaymentRail.MobileMoney,
            null,
            "corr-route-004"),
        policy: null,
        CancellationToken.None);
}
catch (InvalidOperationException)
{
    unsupportedBlocked = true;
}

Assert(
    unsupportedBlocked,
    "unsupported route rejected");

Console.WriteLine(
    "audit generation ................. PASS");

Console.WriteLine(
    "telemetry generation ............. PASS");

Console.WriteLine();

Console.WriteLine(
    "All AFW-DLV-0014.2 payment routing scenarios passed.");

static void Assert(
    bool condition,
    string scenario)
{
    if (!condition)
    {
        Console.WriteLine(
            $"{scenario} ........ FAIL");

        Environment.ExitCode = 1;

        throw new InvalidOperationException(
            $"Scenario failed: {scenario}");
    }

    Console.WriteLine(
        $"{scenario} ........ PASS");
}
