using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Application;

public sealed class MobileMoneyPayoutFundingRecoveryService
{
    private readonly IMobileMoneyPayoutFundingAttemptStore _attemptStore;
    private readonly IReadOnlyList<IMobileMoneyPayoutFundingRecoveryProbe> _probes;
    private readonly TimeProvider _timeProvider;

    public MobileMoneyPayoutFundingRecoveryService(
        IMobileMoneyPayoutFundingAttemptStore attemptStore,
        IEnumerable<IMobileMoneyPayoutFundingRecoveryProbe> probes,
        TimeProvider timeProvider)
    {
        _attemptStore = attemptStore ??
            throw new ArgumentNullException(nameof(attemptStore));
        ArgumentNullException.ThrowIfNull(probes);
        _timeProvider = timeProvider ??
            throw new ArgumentNullException(nameof(timeProvider));

        _probes = probes.ToArray();
        if (_probes.Any(probe => probe is null))
        {
            throw new ArgumentException(
                "Funding recovery probes cannot contain null entries.",
                nameof(probes));
        }
    }

    public async Task<MobileMoneyPayoutFundingRecoveryResult> RecoverAsync(
        RecoverMobileMoneyPayoutFundingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CorrelationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Correlation id cannot be empty.",
                nameof(command));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var attempts = (await _attemptStore.FindByCorrelationIdAsync(
                command.CorrelationId,
                cancellationToken))
            .ToArray();

        if (attempts.Length == 0)
        {
            throw new InvalidOperationException(
                "No funding attempts exist for the requested correlation.");
        }

        if (attempts.Any(attempt =>
                attempt.CorrelationId != command.CorrelationId))
        {
            throw new InvalidOperationException(
                "Funding attempt store returned an attempt for a different correlation.");
        }

        var processing = attempts
            .Where(attempt => attempt.Status == FundingAttemptStatus.Processing)
            .ToArray();

        if (processing.Length == 0)
        {
            return new MobileMoneyPayoutFundingRecoveryResult(
                command.CorrelationId,
                attempts);
        }

        var recoveryPlan = processing
            .Select(attempt => new RecoveryBinding(
                attempt,
                ResolveProbe(attempt.Allocation.SourceType)))
            .ToArray();

        foreach (var binding in recoveryPlan)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (binding.Attempt.ProcessingStartedAtUtc is not { } processingStartedAtUtc)
            {
                throw new InvalidOperationException(
                    "Processing funding attempt is missing its processing timestamp.");
            }

            var providerResult = await binding.Probe.RecoverAsync(
                new FundingRecoveryProviderRequest(
                    binding.Attempt.Id,
                    binding.Attempt.CorrelationId,
                    binding.Attempt.Allocation.SourceId,
                    binding.Attempt.Allocation.SourceType,
                    binding.Attempt.Allocation.AmountMinor,
                    binding.Attempt.Allocation.CurrencyCode,
                    binding.Attempt.ExecutionIdempotencyKey,
                    processingStartedAtUtc),
                cancellationToken);

            if (providerResult is null)
            {
                throw new InvalidOperationException(
                    "Funding recovery probe returned no result.");
            }

            ValidateProviderResult(providerResult);

            if (providerResult.Disposition ==
                FundingRecoveryDisposition.StillProcessing)
            {
                continue;
            }

            var completedAtUtc = _timeProvider.GetUtcNow();

            if (providerResult.Disposition ==
                FundingRecoveryDisposition.Succeeded)
            {
                binding.Attempt.MarkSucceeded(
                    completedAtUtc,
                    providerResult.ProviderReference);
            }
            else
            {
                binding.Attempt.MarkFailed(
                    completedAtUtc,
                    providerResult.FailureCode!,
                    providerResult.ProviderReference);
            }

            await _attemptStore.SaveAsync(
                binding.Attempt,
                cancellationToken);
        }

        return new MobileMoneyPayoutFundingRecoveryResult(
            command.CorrelationId,
            attempts);
    }

    private IMobileMoneyPayoutFundingRecoveryProbe ResolveProbe(
        FundingSourceType sourceType)
    {
        var matches = _probes
            .Where(probe => probe.Supports(sourceType))
            .ToArray();

        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException(
                $"No funding recovery probe supports source type '{sourceType}'."),
            _ => throw new InvalidOperationException(
                $"Multiple funding recovery probes support source type '{sourceType}'.")
        };
    }

    private static void ValidateProviderResult(
        FundingRecoveryProviderResult result)
    {
        switch (result.Disposition)
        {
            case FundingRecoveryDisposition.StillProcessing:
            case FundingRecoveryDisposition.Succeeded:
                if (result.FailureCode is not null)
                {
                    throw new InvalidOperationException(
                        "Non-failed funding recovery result cannot include a failure code.");
                }

                return;

            case FundingRecoveryDisposition.Failed:
                if (string.IsNullOrWhiteSpace(result.FailureCode))
                {
                    throw new InvalidOperationException(
                        "Failed funding recovery result must include a failure code.");
                }

                return;

            default:
                throw new InvalidOperationException(
                    "Funding recovery result contains an unknown disposition.");
        }
    }

    private sealed record RecoveryBinding(
        FundingAttempt Attempt,
        IMobileMoneyPayoutFundingRecoveryProbe Probe);
}
