using AfriWallet.Disputes.Resolution.Application.Abstractions;

namespace AfriWallet.Disputes.Resolution.Application.Policies;

public sealed class ResolutionRetryPolicy
{
    public const int MaxAttempts = 3;

    public bool ShouldRetry(ProviderSubmissionStatus status, int attemptCount)
    {
        if (attemptCount >= MaxAttempts)
            return false;

        return status is ProviderSubmissionStatus.Timeout or ProviderSubmissionStatus.TemporaryFailure;
    }
}
