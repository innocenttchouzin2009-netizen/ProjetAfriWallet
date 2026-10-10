using MobileMoney.Production.Payout.Funding.Abstractions;
using MobileMoney.Production.Payout.Funding.Domain;

namespace MobileMoney.Production.Payout.Funding.Application;

public sealed class MobileMoneyPayoutFundingExecutionService
{
    private readonly IMobileMoneyPayoutFundingAttemptStore _attemptStore;
    private readonly IReadOnlyList<IMobileMoneyPayoutFundingExecutor> _executors;
    private readonly TimeProvider _timeProvider;

    public MobileMoneyPayoutFundingExecutionService(
        IMobileMoneyPayoutFundingAttemptStore attemptStore,
        IEnumerable<IMobileMoneyPayoutFundingExecutor> executors,
        TimeProvider timeProvider)
    {
        _attemptStore = attemptStore ??
            throw new ArgumentNullException(nameof(attemptStore));
        ArgumentNullException.ThrowIfNull(executors);
        _timeProvider = timeProvider ??
            throw new ArgumentNullException(nameof(timeProvider));

        _executors = executors.ToArray();
        if (_executors.Any(executor => executor is null))
        {
            throw new ArgumentException(
                "Funding executors cannot contain null entries.",
                nameof(executors));
        }
    }

    public async Task<MobileMoneyPayoutFundingExecutionResult> ExecuteAsync(
        ExecuteMobileMoneyPayoutFundingCommand command,
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

        if (attempts.Any(attempt =>
                attempt.Status == FundingAttemptStatus.Processing))
        {
            throw new InvalidOperationException(
                "Funding execution contains an in-flight attempt and requires recovery.");
        }

        if (attempts.Any(attempt =>
                attempt.Status is FundingAttemptStatus.Failed
                    or FundingAttemptStatus.Cancelled))
        {
            return new MobileMoneyPayoutFundingExecutionResult(
                command.CorrelationId,
                attempts);
        }

        var planned = attempts
            .Where(attempt => attempt.Status == FundingAttemptStatus.Planned)
            .ToArray();

        if (planned.Length == 0)
        {
            return new MobileMoneyPayoutFundingExecutionResult(
                command.CorrelationId,
                attempts);
        }

        var executionPlan = planned
            .Select(attempt => new ExecutionBinding(
                attempt,
                ResolveExecutor(attempt.Allocation.SourceType)))
            .ToArray();

        foreach (var binding in executionPlan)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var processingAtUtc = _timeProvider.GetUtcNow();
            binding.Attempt.MarkProcessing(processingAtUtc);

            await _attemptStore.SaveAsync(
                binding.Attempt,
                cancellationToken);

            var providerResult = await binding.Executor.ExecuteAsync(
                new FundingExecutionProviderRequest(
                    binding.Attempt.Id,
                    binding.Attempt.CorrelationId,
                    binding.Attempt.Allocation.SourceId,
                    binding.Attempt.Allocation.SourceType,
                    binding.Attempt.Allocation.AmountMinor,
                    binding.Attempt.Allocation.CurrencyCode,
                    binding.Attempt.ExecutionIdempotencyKey,
                    processingAtUtc),
                cancellationToken);

            if (providerResult is null)
            {
                throw new InvalidOperationException(
                    "Funding executor returned no result.");
            }

            ValidateProviderResult(providerResult);

            var completedAtUtc = _timeProvider.GetUtcNow();

            if (providerResult.Succeeded)
            {
                binding.Attempt.MarkSucceeded(
                    completedAtUtc,
                    providerResult.ProviderReference);

                await _attemptStore.SaveAsync(
                    binding.Attempt,
                    cancellationToken);

                continue;
            }

            binding.Attempt.MarkFailed(
                completedAtUtc,
                providerResult.FailureCode!,
                providerResult.ProviderReference);

            await _attemptStore.SaveAsync(
                binding.Attempt,
                cancellationToken);

            break;
        }

        return new MobileMoneyPayoutFundingExecutionResult(
            command.CorrelationId,
            attempts);
    }

    private IMobileMoneyPayoutFundingExecutor ResolveExecutor(
        FundingSourceType sourceType)
    {
        var matches = _executors
            .Where(executor => executor.Supports(sourceType))
            .ToArray();

        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException(
                $"No funding executor supports source type '{sourceType}'."),
            _ => throw new InvalidOperationException(
                $"Multiple funding executors support source type '{sourceType}'.")
        };
    }

    private static void ValidateProviderResult(
        FundingExecutionProviderResult result)
    {
        if (result.Succeeded)
        {
            if (result.FailureCode is not null)
            {
                throw new InvalidOperationException(
                    "Successful funding execution cannot include a failure code.");
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(result.FailureCode))
        {
            throw new InvalidOperationException(
                "Failed funding execution must include a failure code.");
        }
    }

    private sealed record ExecutionBinding(
        FundingAttempt Attempt,
        IMobileMoneyPayoutFundingExecutor Executor);
}
