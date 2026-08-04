using System.Diagnostics;
using System.Diagnostics.Metrics;
using MobileMoney.Production.Correlation;

namespace MobileMoney.Production.Telemetry;

public sealed class MobileMoneyTelemetry : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly Counter<long> _requestsTotal;
    private readonly Counter<long> _successTotal;
    private readonly Counter<long> _failureTotal;
    private readonly Counter<long> _retriesTotal;
    private readonly Counter<long> _timeoutsTotal;
    private readonly Counter<long> _rateLimitedTotal;
    private readonly Counter<long> _callbacksTotal;
    private readonly Histogram<double> _requestDurationMs;
    private readonly Histogram<double> _providerDurationMs;
    private readonly Histogram<double> _transactionCompletionMs;
    private readonly UpDownCounter<int> _inflightRequests;
    private readonly UpDownCounter<int> _pendingTransactions;
    private readonly ObservableGauge<int> _circuitState;

    public MobileMoneyTelemetry(MobileMoneyTelemetryOptions options)
    {
        _activitySource = new ActivitySource(options.ServiceName);
        _meter = new Meter(options.ServiceName, options.ServiceVersion);

        _requestsTotal = _meter.CreateCounter<long>("afw_mobile_money_requests_total");
        _successTotal = _meter.CreateCounter<long>("afw_mobile_money_success_total");
        _failureTotal = _meter.CreateCounter<long>("afw_mobile_money_failure_total");
        _retriesTotal = _meter.CreateCounter<long>("afw_mobile_money_retries_total");
        _timeoutsTotal = _meter.CreateCounter<long>("afw_mobile_money_timeouts_total");
        _rateLimitedTotal = _meter.CreateCounter<long>("afw_mobile_money_rate_limited_total");
        _callbacksTotal = _meter.CreateCounter<long>("afw_mobile_money_callbacks_total");
        _requestDurationMs = _meter.CreateHistogram<double>("afw_mobile_money_request_duration_ms");
        _providerDurationMs = _meter.CreateHistogram<double>("afw_mobile_money_provider_duration_ms");
        _transactionCompletionMs = _meter.CreateHistogram<double>("afw_mobile_money_transaction_completion_ms");
        _inflightRequests = _meter.CreateUpDownCounter<int>("afw_mobile_money_inflight_requests");
        _pendingTransactions = _meter.CreateUpDownCounter<int>("afw_mobile_money_pending_transactions");
        _circuitState = _meter.CreateObservableGauge<int>("afw_mobile_money_circuit_state", () => new[] { new Measurement<int>(_circuitStateValue) });
    }

    private int _circuitStateValue;

    public ActivitySource ActivitySource => _activitySource;
    public Meter Meter => _meter;

    public Activity? StartActivity(string operationName, string? transactionId = null, string? providerReference = null, string? providerCode = null, string? operationType = null, string? currency = null, long? amountMinor = null)
    {
        var activity = _activitySource.StartActivity(operationName, ActivityKind.Internal);
        if (activity is null)
        {
            return null;
        }

        activity.SetTag("afw.environment", "Staging");
        activity.SetTag("afw.provider.code", providerCode);
        activity.SetTag("afw.operation.type", operationType);
        activity.SetTag("afw.transaction.id", transactionId);
        activity.SetTag("afw.provider.reference", providerReference);
        activity.SetTag("afw.currency", currency);
        activity.SetTag("afw.amount.minor", amountMinor);

        if (Activity.Current is not null)
        {
            Activity.Current.SetTag("afw.correlation.id", CorrelationContext.FromHttpContext(null)?.CorrelationId ?? CorrelationIdValidator.DefaultCorrelationId);
        }

        return activity;
    }

    public void RecordRequest() => _requestsTotal.Add(1);
    public void RecordSuccess() => _successTotal.Add(1);
    public void RecordFailure() => _failureTotal.Add(1);
    public void RecordRetry() => _retriesTotal.Add(1);
    public void RecordTimeout() => _timeoutsTotal.Add(1);
    public void RecordRateLimited() => _rateLimitedTotal.Add(1);
    public void RecordCallback() => _callbacksTotal.Add(1);
    public void RecordRequestDuration(double durationMs) => _requestDurationMs.Record(durationMs);
    public void RecordProviderDuration(double durationMs) => _providerDurationMs.Record(durationMs);
    public void RecordTransactionCompletion(double durationMs) => _transactionCompletionMs.Record(durationMs);
    public void IncrementInflight() => _inflightRequests.Add(1);
    public void DecrementInflight() => _inflightRequests.Add(-1);
    public void IncrementPending() => _pendingTransactions.Add(1);
    public void DecrementPending() => _pendingTransactions.Add(-1);
    public void SetCircuitState(int state) => _circuitStateValue = state;

    public void Dispose()
    {
        _activitySource.Dispose();
        _meter.Dispose();
    }
}
