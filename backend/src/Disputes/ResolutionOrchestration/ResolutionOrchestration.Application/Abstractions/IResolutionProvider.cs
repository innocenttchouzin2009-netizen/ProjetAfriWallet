using AfriWallet.Disputes.Resolution.Domain.Resolutions;

namespace AfriWallet.Disputes.Resolution.Application.Abstractions;

public enum ProviderSubmissionStatus
{
    Accepted = 0,
    TemporaryFailure = 1,
    PermanentFailure = 2,
    Timeout = 3,
    PartialFailure = 4
}

public sealed record ResolutionProviderRequest(
    Guid ResolutionId,
    Guid DecisionId,
    ResolutionRoute Route,
    string Awid,
    string IdempotencyKey,
    string CorrelationId);

public sealed record ResolutionProviderResult(
    ProviderSubmissionStatus Status,
    string? ProviderReference,
    string Message);

public interface IResolutionProvider
{
    Task<ResolutionProviderResult> SubmitAsync(ResolutionProviderRequest request, CancellationToken cancellationToken = default);
    Task<bool> CompensateAsync(string providerReference, CancellationToken cancellationToken = default);
}
