using PaymentRouting.Application.Interfaces;
using PaymentRouting.Application.Scoring;
using PaymentRouting.Domain.Decisions;
using PaymentRouting.Domain.Policies;
using PaymentRouting.Domain.Providers;
using PaymentRouting.Domain.Routes;

namespace PaymentRouting.Application.Services;

public sealed class PaymentRoutingService
{
    private readonly IPaymentProviderRepository _providers;
    private readonly IRoutingDecisionRepository _decisions;
    private readonly PaymentRouteScorer _scorer;

    public PaymentRoutingService(
        IPaymentProviderRepository providers,
        IRoutingDecisionRepository decisions,
        PaymentRouteScorer scorer)
    {
        _providers = providers;
        _decisions = decisions;
        _scorer = scorer;
    }

    public async Task<RoutingDecision> RouteAsync(
        RoutingRequest request,
        RoutingPolicy? policy,
        CancellationToken cancellationToken)
    {
        if (request.PaymentIntentId == Guid.Empty)
            throw new ArgumentException(
                "Payment intent ID is required.");

        if (request.AmountMinor <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(request.AmountMinor));

        var existing =
            await _decisions.GetByPaymentIntentAsync(
                request.PaymentIntentId,
                cancellationToken);

        if (existing is not null)
            return existing;

        var effectivePolicy =
            policy ?? RoutingPolicy.Default;

        var providers =
            await _providers.ListAsync(
                cancellationToken);

        var candidates =
            providers
                .Where(x =>
                    x.Rail == request.RequestedRail)
                .Where(x =>
                    x.Status is ProviderStatus.Active
                    or ProviderStatus.Degraded)
                .Where(x =>
                    x.Supports(
                        request.CountryCode,
                        request.CurrencyCode))
                .ToArray();

        if (candidates.Length == 0)
        {
            throw new InvalidOperationException(
                "No eligible payment route is available.");
        }

        var scored =
            candidates
                .Select(provider =>
                    _scorer.Score(
                        provider,
                        effectivePolicy,
                        isFallback: false))
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Priority)
                .ToList();

        if (!string.IsNullOrWhiteSpace(
                request.PreferredProviderId))
        {
            var preferred =
                scored.FirstOrDefault(x =>
                    string.Equals(
                        x.ProviderId,
                        request.PreferredProviderId,
                        StringComparison.OrdinalIgnoreCase));

            if (preferred is not null)
            {
                scored.Remove(preferred);
                scored.Insert(0, preferred);
            }
        }

        var selected = scored[0];

        var alternatives =
            scored
                .Skip(1)
                .Select(x =>
                    x with
                    {
                        IsFallback = true
                    })
                .ToArray();

        var decision =
            new RoutingDecision(
                Guid.NewGuid(),
                request.PaymentIntentId,
                selected,
                alternatives,
                $"Selected {selected.ProviderId} based on routing policy.",
                DateTime.UtcNow);

        await _decisions.AddAsync(
            decision,
            cancellationToken);

        return decision;
    }
}
