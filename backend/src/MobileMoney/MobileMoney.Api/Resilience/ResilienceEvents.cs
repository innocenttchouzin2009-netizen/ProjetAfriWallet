namespace MobileMoney.Production.Resilience;

public static class ResilienceEvents
{
    public const string ExecutionStarted = "PIPELINE_EXECUTION_STARTED";
    public const string ExecutionFinished = "PIPELINE_EXECUTION_FINISHED";
    public const string Retry = "PIPELINE_RETRY";
    public const string Timeout = "PIPELINE_TIMEOUT";
    public const string Fallback = "PIPELINE_FALLBACK";
    public const string CircuitOpen = "PIPELINE_CIRCUIT_OPEN";
    public const string CircuitHalfOpen = "PIPELINE_CIRCUIT_HALF_OPEN";
    public const string CircuitClosed = "PIPELINE_CIRCUIT_CLOSED";
}
