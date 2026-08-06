namespace Device.Contracts;

public sealed class DeviceEvaluationResult
{
    public Guid EvaluationId { get; init; } = Guid.NewGuid();
    public DeviceDecisionType Decision { get; init; }
    public string RiskLevel { get; init; } = "LOW";
    public int Score { get; init; }
    public IReadOnlyList<string> TriggeredSignals { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AuditEvents { get; init; } = Array.Empty<string>();
    public DeviceTelemetry? Telemetry { get; init; }
    public string DeviceFingerprint { get; init; } = string.Empty;
    public bool KnownDevice { get; init; }
    public bool NewDevice { get; init; }
    public int DeviceTrustScore { get; init; }
}

public sealed class DeviceTelemetry
{
    public string Decision { get; init; } = string.Empty;
    public string RiskLevel { get; init; } = string.Empty;
    public int Score { get; init; }
    public int TriggeredSignalCount { get; init; }
    public double EvaluationDurationMs { get; init; }
}
