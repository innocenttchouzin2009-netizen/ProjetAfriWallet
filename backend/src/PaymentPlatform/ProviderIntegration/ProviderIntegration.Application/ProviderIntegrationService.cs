using System.Collections.Concurrent;
using System.Diagnostics;

namespace AfriWallet.PaymentPlatform.ProviderIntegration.Application;

public sealed class ProviderIntegrationService
{
    private readonly IProviderCredentialService _credentials;
    private readonly IProviderExecutor _executor;
    private readonly IProviderHealthService _health;
    private readonly RetryPolicy _retryPolicy;
    private readonly ConcurrentDictionary<string, CircuitBreaker> _breakers =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<ProviderAuditEvent> _audit = new();
    private readonly ConcurrentQueue<ProviderTelemetryEvent> _telemetry = new();

    public ProviderIntegrationService(
        IProviderCredentialService credentials,
        IProviderExecutor executor,
        IProviderHealthService health,
        RetryPolicy retryPolicy)
    {
        _credentials = credentials;
        _executor = executor;
        _health = health;
        _retryPolicy = retryPolicy;
    }

    public IReadOnlyCollection<ProviderAuditEvent> AuditEvents
        => _audit.ToArray();

    public IReadOnlyCollection<ProviderTelemetryEvent> TelemetryEvents
        => _telemetry.ToArray();

    public async Task<ProviderExecutionResult> ExecuteAsync(
        ProviderExecutionRequest request,
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        Validate(request, maxRetries);

        var breaker = _breakers.GetOrAdd(
            request.ProviderCode,
            static _ => new CircuitBreaker());

        if (breaker.IsOpen)
        {
            var circuitResult = new ProviderExecutionResult(
                false,
                null,
                "circuit_open",
                "Provider circuit breaker is open.",
                true);

            RecordEvents(request, circuitResult, 0);
            return circuitResult;
        }

        ProviderCredential credential;

        try
        {
            credential = await _credentials.GetCredentialAsync(
                request.ProviderCode,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            var credentialResult = new ProviderExecutionResult(
                false,
                null,
                "credential_unavailable",
                "Provider credentials are unavailable.",
                false);

            RecordEvents(request, credentialResult, 0);
            return credentialResult;
        }

        if (string.IsNullOrWhiteSpace(credential.AccessToken) ||
            credential.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            var credentialResult = new ProviderExecutionResult(
                false,
                null,
                "credential_invalid",
                "Provider credentials are invalid or expired.",
                false);

            RecordEvents(request, credentialResult, 0);
            return credentialResult;
        }

        var stopwatch = Stopwatch.StartNew();

        var result = await _retryPolicy.ExecuteAsync(
            operation: cancellationTokenValue => ExecuteProviderAsync(
                request,
                credential,
                cancellationTokenValue),
            shouldRetry: execution => !execution.Success && execution.Retryable,
            maxRetries,
            cancellationToken);

        stopwatch.Stop();

        if (result.Success)
        {
            breaker.RecordSuccess();
            _health.RecordSuccess(request.ProviderCode, stopwatch.Elapsed.TotalMilliseconds);
        }
        else
        {
            breaker.RecordFailure();
            _health.RecordFailure(request.ProviderCode, stopwatch.Elapsed.TotalMilliseconds);
        }

        RecordEvents(request, result, stopwatch.Elapsed.TotalMilliseconds);
        return result;
    }

    private async Task<ProviderExecutionResult> ExecuteProviderAsync(
        ProviderExecutionRequest request,
        ProviderCredential credential,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _executor.ExecuteAsync(request, credential, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return new ProviderExecutionResult(
                false,
                null,
                "provider_execution_error",
                "Provider execution failed.",
                true);
        }
    }

    private static void Validate(ProviderExecutionRequest request, int maxRetries)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ProviderCode))
            throw new ArgumentException("ProviderCode is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Operation))
            throw new ArgumentException("Operation is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.CorrelationId))
            throw new ArgumentException("CorrelationId is required.", nameof(request));

        if (request.Payload is null)
            throw new ArgumentException("Payload is required.", nameof(request));

        if (maxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetries));
    }

    private void RecordEvents(
        ProviderExecutionRequest request,
        ProviderExecutionResult result,
        double durationMs)
    {
        _audit.Enqueue(new ProviderAuditEvent(
            "provider.execution.completed",
            request.ProviderCode,
            request.Operation,
            request.CorrelationId,
            result.Success,
            DateTimeOffset.UtcNow));

        _telemetry.Enqueue(new ProviderTelemetryEvent(
            "provider.execution",
            request.ProviderCode,
            result.Success ? "success" : result.ErrorCode ?? "failure",
            durationMs,
            DateTimeOffset.UtcNow));
    }
}