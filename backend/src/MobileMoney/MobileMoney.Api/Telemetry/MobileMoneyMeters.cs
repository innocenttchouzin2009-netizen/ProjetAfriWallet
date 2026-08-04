namespace MobileMoney.Production.Telemetry;

public static class MobileMoneyMeters
{
    public const string RequestsTotal = "afw_mobile_money_requests_total";
    public const string SuccessTotal = "afw_mobile_money_success_total";
    public const string FailureTotal = "afw_mobile_money_failure_total";
    public const string RetriesTotal = "afw_mobile_money_retries_total";
    public const string TimeoutsTotal = "afw_mobile_money_timeouts_total";
    public const string RateLimitedTotal = "afw_mobile_money_rate_limited_total";
    public const string CallbacksTotal = "afw_mobile_money_callbacks_total";
    public const string RequestDurationMs = "afw_mobile_money_request_duration_ms";
    public const string ProviderDurationMs = "afw_mobile_money_provider_duration_ms";
    public const string TransactionCompletionMs = "afw_mobile_money_transaction_completion_ms";
    public const string InflightRequests = "afw_mobile_money_inflight_requests";
    public const string PendingTransactions = "afw_mobile_money_pending_transactions";
    public const string CircuitState = "afw_mobile_money_circuit_state";
}
