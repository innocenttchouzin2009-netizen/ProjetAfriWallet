using AfriWallet.BankingPlatform.BankProviderIntegration.Application.Interfaces;
using AfriWallet.BankingPlatform.BankProviderIntegration.Domain.Transfers;

namespace AfriWallet.BankingPlatform.BankProviderIntegration.Application.Services;

public sealed class BankProviderIntegrationService
{
    private readonly IBankProviderRegistry _providers;
    private readonly IProviderTransferRepository _repository;
    private readonly IProviderTelemetry _telemetry;

    public BankProviderIntegrationService(
        IBankProviderRegistry providers,
        IProviderTransferRepository repository,
        IProviderTelemetry telemetry)
    {
        _providers = providers;
        _repository = repository;
        _telemetry = telemetry;
    }

    public async Task<ProviderTransfer> SubmitAsync(
        SubmitProviderTransferRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var provider = _providers.GetRequired(request.ProviderCode);
        if (provider.Environment != Domain.Providers.BankProviderEnvironment.Sandbox)
        {
            throw new InvalidOperationException(
                "Only sandbox bank providers are allowed.");
        }

        var adapter = _providers.GetAdapter(request.ProviderCode);
        var transfer = new ProviderTransfer(
            Guid.NewGuid(),
            request.ExecutionId,
            request.ProviderCode,
            request.RailCode,
            request.AmountMinor,
            request.CurrencyCode,
            request.IdempotencyKey);

        await _repository.AddAsync(transfer, cancellationToken);
        transfer.MarkSubmitting();
        _telemetry.SubmissionStarted(request.ProviderCode);

        var result = await adapter.SubmitAsync(request, cancellationToken);
        if (!result.Success)
        {
            transfer.MarkFailed();
            _telemetry.SubmissionFailed(
                request.ProviderCode,
                result.ErrorCode ?? "provider_error");
            return transfer;
        }

        if (string.IsNullOrWhiteSpace(result.ProviderReference))
        {
            transfer.MarkFailed();
            _telemetry.SubmissionFailed(
                request.ProviderCode,
                "provider_reference_missing");
            return transfer;
        }

        transfer.MarkSubmitted(result.ProviderReference);
        _telemetry.SubmissionSucceeded(request.ProviderCode);
        return transfer;
    }

    public Task<ProviderTransfer?> GetAsync(
        Guid providerTransferId,
        CancellationToken cancellationToken)
        => _repository.GetAsync(providerTransferId, cancellationToken);
}
