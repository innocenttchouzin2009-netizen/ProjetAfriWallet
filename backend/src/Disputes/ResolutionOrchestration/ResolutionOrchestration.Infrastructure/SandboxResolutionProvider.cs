using AfriWallet.Disputes.Resolution.Application.Abstractions;

namespace AfriWallet.Disputes.Resolution.Infrastructure;

public sealed class SandboxResolutionProvider : IResolutionProvider
{
    private readonly Queue<ProviderSubmissionStatus> behaviors = new();

    public void Enqueue(ProviderSubmissionStatus status) => behaviors.Enqueue(status);

    public Task<ResolutionProviderResult> SubmitAsync(ResolutionProviderRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var behavior = behaviors.Count > 0 ? behaviors.Dequeue() : ProviderSubmissionStatus.Accepted;
        var providerReference = behavior is ProviderSubmissionStatus.Accepted or ProviderSubmissionStatus.PartialFailure
            ? $"SANDBOX-{request.ResolutionId:N}"
            : null;

        return Task.FromResult(new ResolutionProviderResult(behavior, providerReference, $"Sandbox provider result: {behavior}"));
    }

    public Task<bool> CompensateAsync(string providerReference, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(!string.IsNullOrWhiteSpace(providerReference));
    }
}
