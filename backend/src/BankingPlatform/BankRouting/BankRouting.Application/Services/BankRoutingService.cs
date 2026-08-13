using BankRouting.Application.Contracts;
using BankRouting.Application.Interfaces;
using BankRouting.Domain.Policies;
using BankRouting.Domain.Rails;
using BankRouting.Domain.Routing;

using RoutingContract = BankRouting.Application.Contracts.RoutingRequest;

namespace BankRouting.Application.Services;

public sealed class BankRoutingService
{    private readonly IBankRailRegistry _railRegistry;
    private readonly IBankRoutingDecisionRepository _decisionRepository;

    public BankRoutingService(
        IBankRailRegistry railRegistry,
        IBankRoutingDecisionRepository decisionRepository)
    {
        _railRegistry = railRegistry;
        _decisionRepository = decisionRepository;
    }

    public async Task<RoutingDecision> EvaluateAsync(
        RoutingContract request,
        CancellationToken cancellationToken = default)
    {
        if (request.TransferIntentId == Guid.Empty)
            throw new ArgumentException("Transfer intent ID is required.");

        if (request.AmountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.AmountMinor));

        var existingIdempotent = await _decisionRepository.GetByIdempotencyKeyAsync(
            request.IdempotencyKey,
            cancellationToken);

        if (existingIdempotent is not null)
            return existingIdempotent;

        var existingTransfer = await _decisionRepository.GetByTransferIntentAsync(
            request.TransferIntentId,
            cancellationToken);

        if (existingTransfer is not null)
            return existingTransfer;

        var rails = await _railRegistry.ListAsync(cancellationToken);

        var candidates = rails
            .Where(r => r.Supports(request.CountryCode, request.CurrencyCode, request.AmountMinor))
            .Where(r => r.IsActive && r.IsHealthy)
            .ToArray();

        if (candidates.Length == 0)
            throw new InvalidOperationException("No eligible bank rail is available for the provided transfer.");

        var preferred = request.PreferredRail is not null
            ? candidates.FirstOrDefault(r => string.Equals(r.RailType.ToString(), request.PreferredRail, StringComparison.OrdinalIgnoreCase))
            : null;

        var ordered = preferred is not null
            ? candidates.OrderByDescending(r => r.RailId == preferred.RailId ? 1 : 0)
                .ThenByDescending(r => r.RailType == BankRailType.SepaInstant ? 1 : 0)
                .ThenBy(r => r.Priority)
                .ThenBy(r => r.EstimatedCostMinor)
            : candidates
                .OrderByDescending(r => r.RailType == BankRailType.SepaInstant ? 1 : 0)
                .ThenBy(r => r.Priority)
                .ThenBy(r => r.EstimatedCostMinor);

        var selected = ordered.First();
        var fallback = ordered
            .Skip(1)
            .Take(RoutingPolicy.Default.MaxFallbackCount)
            .Select(r => r.RailType)
            .ToArray();

        var reason = selected.RailType switch
        {
            BankRailType.SepaInstant => "SEPA Instant selected because it is eligible, healthy, and preferred for EU instant settlement.",
            BankRailType.Sepa => "SEPA selected because it is eligible and available for the destination country and currency.",
            BankRailType.Swift => "SWIFT selected as the eligible international rail for the requested transfer.",
            BankRailType.LocalBankTransfer => "Local bank transfer selected for the configured market and currency.",
            _ => "Eligible rail selected by deterministic scoring."
        };

        var score = 100
            + selected.Priority * 10
            + (selected.IsHealthy ? 20 : 0)
            + (selected.RailType == BankRailType.SepaInstant ? 25 : 0)
            - (int)Math.Round(selected.EstimatedCostMinor / 100m);

        var decision = new RoutingDecision(
            DecisionId: Guid.NewGuid(),
            TransferIntentId: request.TransferIntentId,
            OwnerAwid: request.OwnerAwid,
            IdempotencyKey: request.IdempotencyKey,
            SelectedRail: selected.RailType,
            FallbackRails: fallback,
            Reason: reason,
            Score: score,
            EstimatedCostMinor: selected.EstimatedCostMinor,
            CreatedAtUtc: DateTime.UtcNow);

        await _decisionRepository.AddAsync(decision, cancellationToken);
        return decision;
    }
}
