using Microsoft.Extensions.Options;
using MobileMoney.Production.Configuration;
using MobileMoney.Production.Logging;

namespace MobileMoney.Production.Resilience;

public sealed class ResiliencePipelineFactory
{
    private readonly ResilienceOptions _options;
    private readonly StructuredOperationLogger _logger;

    public ResiliencePipelineFactory(IOptions<ResilienceOptions> options, StructuredOperationLogger logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public ResiliencePipeline Create(string providerName)
    {
        var telemetry = new ResilienceTelemetry();
        var breaker = new CircuitBreakerFactory().Build(_options);
        return new ResiliencePipeline(providerName, _options, telemetry, breaker, _logger);
    }
}

public sealed class ResiliencePipeline
{
    private readonly string _providerName;
    private readonly ResilienceOptions _options;
    private readonly ResilienceTelemetry _telemetry;
    private readonly CircuitBreakerState _breaker;
    private readonly StructuredOperationLogger _logger;

    public ResiliencePipeline(string providerName, ResilienceOptions options, ResilienceTelemetry telemetry, CircuitBreakerState breaker, StructuredOperationLogger logger)
    {
        _providerName = providerName;
        _options = options;
        _telemetry = telemetry;
        _breaker = breaker;
        _logger = logger;
    }

    public async Task<object> ExecuteAsync(Func<Task<object>> action)
    {
        _logger.LogResilienceEvent(ResilienceEvents.ExecutionStarted, _providerName, _telemetry);
        for (var attempt = 1; attempt <= _options.Retry.MaximumAttempts; attempt++)
        {
            if (!_breaker.CanExecute())
            {
                _logger.LogResilienceEvent(ResilienceEvents.CircuitOpen, _providerName, _telemetry);
                _telemetry.RecordFallback();
                return FallbackPolicyFactory.BuildFallback(_providerName);
            }

            try
            {
                using var timeout = new CancellationTokenSource(TimeoutPolicyFactory.BuildRequestTimeout(_options));
                var result = await action();
                _breaker.RecordSuccess();
                _logger.LogResilienceEvent(ResilienceEvents.ExecutionFinished, _providerName, _telemetry);
                return result;
            }
            catch (TimeoutException)
            {
                _telemetry.RecordTimeout();
                _logger.LogResilienceEvent(ResilienceEvents.Timeout, _providerName, _telemetry);
                if (attempt < _options.Retry.MaximumAttempts)
                {
                    _telemetry.RecordRetry();
                    _logger.LogResilienceEvent(ResilienceEvents.Retry, _providerName, _telemetry);
                    await Task.Delay(RetryPolicyFactory.BuildDelay(attempt, _options));
                    continue;
                }

                _telemetry.RecordFallback();
                return FallbackPolicyFactory.BuildFallback(_providerName);
            }
            catch (Exception)
            {
                _breaker.RecordFailure();
                _telemetry.RecordFallback();
                if (attempt < _options.Retry.MaximumAttempts)
                {
                    _telemetry.RecordRetry();
                    _logger.LogResilienceEvent(ResilienceEvents.Retry, _providerName, _telemetry);
                    await Task.Delay(RetryPolicyFactory.BuildDelay(attempt, _options));
                    continue;
                }

                _logger.LogResilienceEvent(ResilienceEvents.Fallback, _providerName, _telemetry);
                return FallbackPolicyFactory.BuildFallback(_providerName);
            }
        }

        _logger.LogResilienceEvent(ResilienceEvents.Fallback, _providerName, _telemetry);
        return FallbackPolicyFactory.BuildFallback(_providerName);
    }
}
